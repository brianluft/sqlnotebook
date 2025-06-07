using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
using SqlNotebookScript;

namespace SqlNotebook;

public partial class WaitForm : ZForm
{
    private readonly Stopwatch _stopwatch;
    private readonly Pen _spinnerPen;
    private readonly SolidBrush _spinnerBrush;
    private readonly float _spinnerPenWidth;
    private readonly System.Windows.Forms.Timer _timer;

    // Set this at startup.
    public static MainForm MainAppForm { private get; set; }

    public event EventHandler CancelRequested;
    public Task WaitTask { get; }
    public Exception ResultException { get; private set; }
    public bool DidCancel { get; private set; }

    public static void Go(IWin32Window owner, string title, string text, out bool success, Action action) =>
        GoCore(owner, title, text, out success, action);

    public static T Go<T>(IWin32Window owner, string title, string text, out bool success, Func<T> func)
    {
        T result = default;
        Go(
            owner,
            title,
            text,
            out success,
            action: () =>
            {
                result = func();
            }
        );
        return result;
    }

    public static void GoWithCancel(
        IWin32Window owner,
        string title,
        string text,
        out bool success,
        Action<CancellationToken> action
    )
    {
        using CancellationTokenSource cts = new();
        GoCore(owner, title, text, out success, () => action(cts.Token), cancelAction: () => cts.Cancel());
    }

    public static T GoWithCancel<T>(
        IWin32Window owner,
        string title,
        string text,
        out bool success,
        Func<CancellationToken, T> func
    )
    {
        T result = default;
        GoWithCancel(
            owner,
            title,
            text,
            out success,
            action: cancel =>
            {
                result = func(cancel);
            }
        );
        return result;
    }

    // The action is uncancelable, so just let it finish and forget about it. This is the worst kind of
    // cancellation so we give it an irritating name to discourage its use.
    public static void GoWithCancelByWalkingAway(
        IWin32Window owner,
        string title,
        string text,
        out bool success,
        Action action
    )
    {
        success = false;

        // Try running the action briefly before pulling up the wait form, to avoid a flicker of the wait form for
        // very fast tasks.
        var task = Task.Run(action);
        try
        {
            task.Wait(millisecondsTimeout: 100);
        }
        catch (Exception ex)
        {
            Ui.ShowError(owner, title, ex);
            return;
        }

        if (task.IsCompleted)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Ui.ShowError(owner, title, ex);
                return;
            }
        }
        else
        {
            using CancellationTokenSource cts = new();
            using WaitForm f = new(
                title,
                text,
                () =>
                {
                    task.Wait(cts.Token);
                    cts.Token.ThrowIfCancellationRequested();
                    task.GetAwaiter().GetResult();
                },
                allowCancel: true
            );
            f.CancelRequested += delegate
            {
                cts.Cancel();
            };
            var result = f.ShowDialogWithPositioning(owner);
            if (result != DialogResult.OK)
            {
                if (f.ResultException is not OperationCanceledException)
                {
                    Ui.ShowError(owner, title, f.ResultException);
                }
                return;
            }
        }

        success = true;
        return;
    }

    private static void GoCore(
        IWin32Window owner,
        string title,
        string text,
        out bool success,
        Action action,
        Action cancelAction = null
    )
    {
        success = false;
        var sw = Stopwatch.StartNew();

        // Try running the action briefly before pulling up the wait form, to avoid a flicker of the wait form for
        // very fast tasks.
        var task = Task.Run(action);
        try
        {
            task.Wait(millisecondsTimeout: 100);
        }
        catch (Exception ex)
        {
            Ui.ShowError(owner, title, ex);
            return;
        }

        if (task.IsCompleted)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Ui.ShowError(owner, title, ex);
                return;
            }
        }
        else
        {
            using WaitForm f = new(title, text, () => task.GetAwaiter().GetResult(), allowCancel: cancelAction != null);
            f.CancelRequested += delegate
            {
                cancelAction?.Invoke();
            };
            var result = f.ShowDialogWithPositioning(owner);
            if (result == DialogResult.Cancel)
            {
                success = false;
                return;
            }
            if (result != DialogResult.OK)
            {
                Ui.ShowError(owner, title, f.ResultException);
                return;
            }
        }

        success = true;
        MainAppForm?.SetFinished(sw.Elapsed);
        return;
    }

    private DialogResult ShowDialogWithPositioning(IWin32Window owner)
    {
        DialogResult result;
        if (owner != null)
        {
            result = ShowDialog(owner);
        }
        else
        {
            StartPosition = FormStartPosition.CenterScreen;
            result = ShowDialog();
        }

        return result;
    }

    private WaitForm(string title, string text, Action action, bool allowCancel)
    {
        InitializeComponent();
        Text = title;
        _infoTxt.Text = text;
        _stopwatch = Stopwatch.StartNew();

        Ui ui = new(this, 75, allowCancel ? 12 : 10, padded: false);
        ui.Init(_table);
        ui.Init(_infoTxt);
        _infoTxt.Margin = Padding.Empty;
        ui.MarginLeft(_infoTxt, 1);
        ui.MarginTop(_infoTxt);
        ui.MarginBottom(_infoTxt);
        ui.Init(_progressLabel);
        _progressLabel.Margin = Padding.Empty;
        ui.MarginLeft(_progressLabel);
        ui.MarginBottom(_progressLabel);
        ui.Init(_spinner);
        ui.MarginLeft(_spinner);
        ui.MarginTop(_spinner, 0.95);
        if (allowCancel)
        {
            ui.Init(_buttonFlow);
            ui.Init(_cancelButton);
            ui.MarginTop(_cancelButton);
            ui.MarginRight(_cancelButton);
            ui.MarginBottom(_cancelButton);
        }
        else
        {
            _buttonFlow.Visible = false;
        }
        _infoTxt.ForeColor = Color.FromArgb(0, 51, 171);
        _spinner.Size = new(ui.XWidth(3), ui.XWidth(3));
        _spinner.EnableDoubleBuffering();
        _spinnerPenWidth = this.Scaled(1.5);
        _spinnerPen = new(Color.FromArgb(0, 51, 171), _spinnerPenWidth);
        _spinnerBrush = new(Color.FromArgb(0, 51, 171));

        Disposed += delegate
        {
            _spinnerPen.Dispose();
        };

        WaitTask = Task.Run(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ResultException = ex;
            }
            while (!IsHandleCreated && !IsDisposed)
            {
                Thread.Sleep(1);
            }
            if (!IsDisposed)
            {
                BeginInvoke(
                    new MethodInvoker(() =>
                    {
                        if (DidCancel)
                        {
                            DialogResult = DialogResult.Cancel;
                        }
                        else
                        {
                            DialogResult = ResultException == null ? DialogResult.OK : DialogResult.Abort;
                        }
                        Close();
                    })
                );
            }
        });

        _timer = new() { Interval = 16, Enabled = true };

        _timer.Tick += delegate
        {
            if (!DidCancel)
            {
                var progressText = WaitStatus.Status;
                if (progressText != null && progressText != _progressLabel.Text)
                {
                    if (!_progressLabel.Visible)
                    {
                        _progressLabel.Visible = true;
                    }
                    _progressLabel.Text = progressText;
                }
                if (progressText == null && _progressLabel.Visible)
                {
                    _progressLabel.Visible = false;
                }
            }
        };

        if (TaskbarManager.IsPlatformSupported && MainAppForm != null && MainAppForm.IsHandleCreated)
        {
            // It seems like the indeterminate progressbar state isn't shown on Windows 10. Let's fake a progress
            // by asymptotically approaching 90% with a hand-tweaked curve that seems to be OK most of the time.
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, MainAppForm.Handle);
            var stopwatch = Stopwatch.StartNew();
            var lastValue = 0;
            _timer.Tick += delegate
            {
                try
                {
                    var value = (int)(90 * (1 - Math.Exp(0.01 * stopwatch.ElapsedMilliseconds * Math.Log(0.9))));
                    if (value != lastValue)
                    {
                        TaskbarManager.Instance.SetProgressValue(value, 100);
                        lastValue = value;
                    }
                }
                catch { }
            };
            FormClosed += delegate
            {
                try
                {
                    _timer.Stop();
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, MainAppForm.Handle);
                }
                catch { }
            };
            Disposed += delegate
            {
                _timer.Dispose();
            };
        }
    }

    private void Spinner_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var size = _spinner.ClientSize;
        var radius = 0.9 * (size.Width - 2 * _spinnerPenWidth) / 2;

        // Background
        e.Graphics.FillRectangle(Brushes.White, e.ClipRectangle);

        // Sweep through 360 degrees once per second
        var fraction = _stopwatch.Elapsed.TotalSeconds;
        fraction -= Math.Floor(fraction);
        var tailDegrees = fraction * 360;

        for (var i = 0; i < 2; i++)
        {
            var headDegrees = tailDegrees - 70;

            RectangleF rect = new(
                (float)(1.5 * _spinnerPenWidth),
                (float)(1.5 * _spinnerPenWidth),
                size.Width - 3 * _spinnerPenWidth,
                size.Height - 3 * _spinnerPenWidth
            );
            e.Graphics.DrawArc(_spinnerPen, rect, (float)tailDegrees, 100);

            var x = radius * Math.Cos(Math.PI * headDegrees / 180) + (double)_spinner.ClientSize.Width / 2;
            var y = radius * Math.Sin(Math.PI * headDegrees / 180) + (double)_spinner.ClientSize.Height / 2;
            e.Graphics.FillEllipse(
                _spinnerBrush,
                (float)(x - 1.5 * _spinnerPenWidth),
                (float)(y - 1.5 * _spinnerPenWidth),
                3 * _spinnerPenWidth,
                3 * _spinnerPenWidth
            );

            tailDegrees += 180;
            tailDegrees %= 360;
        }
    }

    private void SpinnerTimer_Tick(object sender, EventArgs e)
    {
        _spinner.Invalidate();
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        if (DidCancel)
        {
            return;
        }
        DidCancel = true;
        _timer.Stop();
        _infoTxt.Text = "Cancelling...";
        _progressLabel.Visible = false;
        _cancelButton.Enabled = false;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
