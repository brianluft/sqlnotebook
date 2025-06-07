// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{
    // This singleton object is correctly finalized on appdomain unload.
    internal class ServiceCache : CriticalFinalizerObject
    {
        private static readonly ServiceCache staticInstance = new ServiceCache();

        // The lock
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        // Specifies if the object has been finalized:
        private int _finalized;

        // Guid -> IntPtr
        private Dictionary<Guid, IntPtr> _guidToService = new Dictionary<Guid, IntPtr>();

        // Active resources refcount, signed 64-bit
        private long _resourceRefCount;

        // IntPtr -> this (serves as a set)
        private List<IntPtr> _servicePointers = new List<IntPtr>();

        // Left empty for singleton enforcement
        private ServiceCache() { }

        ~ServiceCache()
        {
            ReleaseHandle();
        }

        internal static ServiceCache Instance => staticInstance;

        private bool IsInvalid => Interlocked.CompareExchange(ref _finalized, 1, 1) != 0;

        internal IntPtr GetCachedService(ref Guid guid)
        {
            _cacheLock.EnterReadLock();
            try
            {
                _guidToService.TryGetValue(guid, out var result);
                return result;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        internal bool RegisterResource()
        {
            // The correctness of this code relies on the fact that there can be no Int64.MaxValue / 2 executing this code at the same time.
            if (Interlocked.Increment(ref _resourceRefCount) > long.MaxValue / 2)
            {
                Interlocked.Decrement(ref _resourceRefCount);
                return false;
            }
            return true;
        }

        internal void RegisterServices(ref IntPtr originalPtr, IntPtr[] services)
        {
            var addedToCache = false;
            var succeeded = false;
            var length = services.Length;
            // We are taking the write lock to make this function atomic. Thus, enumerating services will generally be a slow operation
            // because of this bottleneck. However, creating a service by GUID should still be relatively fast, since this function will be
            // called only the first time(s) the service is initialized. Note that the write lock is being released in the Cleanup() method.
            _cacheLock.EnterWriteLock();
            try
            {
                TryRegisterServices(originalPtr, services, ref addedToCache);
                succeeded = true;
                if (addedToCache)
                {
                    // Added to cache means that we have added at least one of the services to the service cache. In this case, we need to
                    // set the originalPtr to IntPtr.Zero, in order to tell the caller not to free the native pointer.
                    originalPtr = IntPtr.Zero;
                }
            }
            finally
            {
                if (!succeeded)
                {
                    RollBack(originalPtr, length);
                }
                _cacheLock.ExitWriteLock();
            }
        }

        internal void UnregisterResource()
        {
            if (Interlocked.Decrement(ref _resourceRefCount) == 0 && IsInvalid)
            {
                FreeAllServices();
            }
        }

        private void FreeAllServices()
        {
            // Don't use synchronization here. This will only be called during finalization and at that point synchronization doesn't matter.
            // Also, the lock object might have already been finalized.
            if (_servicePointers != null)
            {
                foreach (var servicePtr in _servicePointers)
                {
                    Win32NativeMethods.MappingFreeServicesVoid(servicePtr);
                }
                _servicePointers = null;
                _guidToService = null;
            }
        }

        private void ReleaseHandle()
        {
            if (!IsInvalid)
            {
                if (Interlocked.Read(ref _resourceRefCount) == 0)
                {
                    FreeAllServices();
                }
                Interlocked.CompareExchange(ref _finalized, 1, 0);
            }
        }

        private void RollBack(IntPtr pServices, int length)
        {
            var succeeded = false;
            try
            {
                // First, remove the original pointer from the cleanup list. The caller of RegisterServices() will take care of freeing it.
                _servicePointers.Remove(pServices);
                // Then, attempt to recover the state of the _guidToService Dictionary. This should not fail.
                for (var i = 0; i < length; ++i)
                {
                    var guid = (Guid)Marshal.PtrToStructure(
                        (IntPtr)((ulong)pServices + InteropTools.OffsetOfGuidInService),
                        InteropTools.TypeOfGuid);
                    _guidToService.Remove(guid);
                    pServices = (IntPtr)((ulong)pServices + InteropTools.SizeOfService);
                }
                succeeded = true;
            }
            finally
            {
                if (!succeeded)
                {
                    // This should never happen, as none of the above functions should be allocating any memory, and this rollback should
                    // generally happen in a low-memory condition. In this case, keep the _servicePointers cleanup list, but invalidate the
                    // whole cache, since we may still have traces of the original pointer there.
                    _guidToService = null;
                }
            }
        }

        private void TryRegisterServices(IntPtr originalPtr, IntPtr[] services, ref bool addedToCache)
        {
            // Here, we will try to add the newly enumerated services to the service cache.

            var pServices = originalPtr;
            for (var i = 0; i < services.Length; ++i)
            {
                var guid = (Guid)Marshal.PtrToStructure(
                    (IntPtr)((ulong)pServices + InteropTools.OffsetOfGuidInService), InteropTools.TypeOfGuid);
                _guidToService.TryGetValue(guid, out var cachedValue);
                if (cachedValue == IntPtr.Zero)
                {
                    _guidToService.Add(guid, pServices);
                    cachedValue = pServices;
                    addedToCache = true;
                }
                System.Diagnostics.Debug.Assert(cachedValue != IntPtr.Zero, "Cached value is NULL");
                services[i] = cachedValue;
                pServices = (IntPtr)((ulong)pServices + InteropTools.SizeOfService);
            }
            if (addedToCache)
            {
                // This means that at least one of the services was stored in the cache. So we must keep the original pointer in our cleanup list.
                _servicePointers.Add(originalPtr);
            }
        }
    }
}