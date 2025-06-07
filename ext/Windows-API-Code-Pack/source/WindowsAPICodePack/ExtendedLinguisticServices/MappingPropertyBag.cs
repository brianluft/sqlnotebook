// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{
    /// <summary>Contains the text recognition data properties retrieved by MappingService.RecognizeText or MappingService.BeginRecognizeText.</summary>
    public class MappingPropertyBag : CriticalFinalizerObject, IDisposable
    {
        internal IntPtr _options = IntPtr.Zero;
        internal GCHandle _text;
        internal Win32PropertyBag _win32PropertyBag;
        private readonly ServiceCache _serviceCache;
        private int _isFinalized;

        internal MappingPropertyBag(MappingOptions options, string text)
        {
            _serviceCache = ServiceCache.Instance;
            if (!_serviceCache.RegisterResource())
            {
                throw new LinguisticException();
            }
            _win32PropertyBag._size = InteropTools.SizeOfWin32PropertyBag;
            if (options != null)
            {
                _options = InteropTools.Pack(ref options._win32Options);
            }
            _text = GCHandle.Alloc(text, GCHandleType.Pinned);
        }

        /// <summary>Frees all unmanaged resources allocated for the property bag, if needed.</summary>
        ~MappingPropertyBag()
        {
            Dispose(false);
        }

        /// <summary>Frees all unmanaged resources allocated for the property bag.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Formats the low-level data contained in this <see cref="MappingPropertyBag">MappingPropertyBag</see> using an implementation of
        /// the <see cref="IMappingFormatter{T}">IMappingFormatter</see> interface.
        /// </summary>
        /// <typeparam name="T">The type with which <see cref="IMappingFormatter{T}">IMappingFormatter</see> is parameterized.</typeparam>
        /// <param name="formatter">The formatter to be used in the formatting.</param>
        /// <returns></returns>
        public T[] FormatData<T>(IMappingFormatter<T> formatter)
        {
            if (formatter == null) { throw new ArgumentNullException("formatter"); }
            return formatter.FormatAll(this);
        }

        /// <summary>
        /// An array of <see cref="MappingDataRange">MappingDataRange</see> objects containing all recognized text range results. This member
        /// is populated by MappingService.RecognizeText or asynchronously with MappingService.BeginRecognizeText.
        /// </summary>
        public MappingDataRange[] GetResultRanges()
        {
            var result = new MappingDataRange[_win32PropertyBag._rangesCount];
            for (var i = 0; i < result.Length; ++i)
            {
                var range = new MappingDataRange
                {
                    _win32DataRange = InteropTools.Unpack<Win32DataRange>(
                    (IntPtr)((ulong)_win32PropertyBag._ranges + ((ulong)i * InteropTools.SizeOfWin32DataRange)))
                };
                result[i] = range;
            }
            return result;
        }

        /// <summary>Clean up both managed and native resources.</summary>
        /// <param name="disposed"></param>
        protected virtual void Dispose(bool disposed)
        {
            if (Interlocked.CompareExchange(ref _isFinalized, 0, 0) == 0)
            {
                var result = DisposeInternal();
                if (result)
                {
                    _serviceCache.UnregisterResource();
                    InteropTools.Free<Win32Options>(ref _options);
                    _text.Free();
                    Interlocked.CompareExchange(ref _isFinalized, 1, 0);
                }
            }
        }

        private bool DisposeInternal()
        {
            if (_win32PropertyBag._context == IntPtr.Zero)
            {
                return true;
            }
            var hResult = Win32NativeMethods.MappingFreePropertyBag(ref _win32PropertyBag);
            if (hResult != 0)
            {
                throw new LinguisticException(hResult);
            }
            return true;
        }
    }
}