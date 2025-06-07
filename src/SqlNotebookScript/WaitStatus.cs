using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SqlNotebookScript;

/// <summary>
/// Used by WaitForm to get insight into the currently executing statement.
/// </summary>
public static class WaitStatus
{
    private static readonly List<InFlight> _stack = new(); // locked

    public static string Status => _stack.Count == 0 ? null : _stack[^1].Status;

    public static InFlight StartStatic(string target)
    {
        lock (_stack)
        {
            InFlightCustom f = new(null);
            f.SetProgress(target);
            _stack.Add(f);
            return f;
        }
    }

    public static InFlightCustom StartCustom(string target)
    {
        lock (_stack)
        {
            InFlightCustom f = new(target);
            _stack.Add(f);
            return f;
        }
    }

    public static InFlightRows StartRows(string target)
    {
        lock (_stack)
        {
            InFlightRows f = new(target);
            _stack.Add(f);
            return f;
        }
    }

    public abstract class InFlight : IDisposable
    {
        private readonly string _target;
        private string _progress;
        private bool _disposedValue;

        public string Status { get; protected set; }

        public InFlight(string target)
        {
            if (target != null && target.Length > 50)
            {
                target = target[..50];
            }
            _target = target;
            UpdateStatus();
        }

        public void SetProgress(string progress)
        {
            _progress = progress;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_progress == null)
            {
                Status = "";
            }
            else if (_target == null)
            {
                Status = _progress;
            }
            else
            {
                Status = _target + Environment.NewLine + _progress;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                lock (_stack)
                {
                    var success = _stack.Remove(this);
                    Debug.Assert(success);
                }

                // set large fields to null
                _disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~InFlight()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class InFlightCustom : InFlight
    {
        public InFlightCustom(string target)
            : base(target) { }
    }

    public class InFlightRows : InFlight
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Thread _thread;
        private long _count; // interlocked

        public InFlightRows(string target)
            : base(target)
        {
            _thread = new(
                new ThreadStart(() =>
                {
                    try
                    {
                        var interval = TimeSpan.FromMilliseconds(100);
                        long previous = -1;
                        while (!_cts.IsCancellationRequested)
                        {
                            var n = Interlocked.Read(ref _count);
                            if (n != previous)
                            {
                                if (n == 0)
                                {
                                    SetProgress(null);
                                }
                                else
                                {
                                    SetProgress($"{n:#,##0} rows");
                                }
                                previous = n;
                            }
                            _cts.Token.WaitHandle.WaitOne(interval);
                        }
                    }
                    catch { }
                })
            );
            _thread.Start();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _cts.Cancel();
            _thread.Join();
            _cts.Dispose();
        }

        public void IncrementRows() => Interlocked.Increment(ref _count);
    }
}
