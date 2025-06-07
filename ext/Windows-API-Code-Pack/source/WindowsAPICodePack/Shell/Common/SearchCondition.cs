//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>Exposes properties and methods for retrieving information about a search condition.</summary>
    public class SearchCondition : IDisposable
    {
        private readonly string canonicalName;

        private readonly SearchConditionOperation conditionOperation = SearchConditionOperation.Implicit;

        private readonly SearchConditionType conditionType = SearchConditionType.Leaf;

        private PropertyKey emptyPropertyKey = new PropertyKey();

        private PropertyKey propertyKey;

        internal SearchCondition(ICondition nativeSearchCondition)
        {
            if (nativeSearchCondition == null)
            {
                throw new ArgumentNullException("nativeSearchCondition");
            }

            NativeSearchCondition = nativeSearchCondition;

            var hr = NativeSearchCondition.GetConditionType(out conditionType);

            if (!CoreErrorHelper.Succeeded(hr))
            {
                throw new ShellException(hr);
            }

            if (ConditionType == SearchConditionType.Leaf)
            {
                using (var propVar = new PropVariant())
                {
                    hr = NativeSearchCondition.GetComparisonInfo(out canonicalName, out conditionOperation, propVar);

                    if (!CoreErrorHelper.Succeeded(hr))
                    {
                        throw new ShellException(hr);
                    }

                    PropertyValue = propVar.Value.ToString();
                }
            }
        }

        /// <summary></summary>
        ~SearchCondition()
        {
            Dispose(false);
        }

        /// <summary>
        /// Search condition operation to be performed on the property/value combination. See
        /// <see cref="Microsoft.WindowsAPICodePack.Shell.SearchConditionOperation"/> for more details.
        /// </summary>
        public SearchConditionOperation ConditionOperation => conditionOperation;

        /// <summary>Represents the condition type for the given node.</summary>
        public SearchConditionType ConditionType => conditionType;

        /// <summary>The name of a property to be compared or NULL for an unspecified property.</summary>
        public string PropertyCanonicalName => canonicalName;

        /// <summary>The property key for the property that is to be compared.</summary>
        public PropertyKey PropertyKey
        {
            get
            {
                if (propertyKey == emptyPropertyKey)
                {
                    var hr = PropertySystemNativeMethods.PSGetPropertyKeyFromName(PropertyCanonicalName, out propertyKey);
                    if (!CoreErrorHelper.Succeeded(hr))
                    {
                        throw new ShellException(hr);
                    }
                }

                return propertyKey;
            }
        }

        /// <summary>A value (in <see cref="System.String"/> format) to which the property is compared.</summary>
        public string PropertyValue { get; internal set; }

        internal ICondition NativeSearchCondition { get; set; }

        /// <summary>Release the native objects.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Retrieves an array of the sub-conditions.</summary>
        public IEnumerable<SearchCondition> GetSubConditions()
        {
            // Our list that we'll return
            var subConditionsList = new List<SearchCondition>();

            // Get the sub-conditions from the native API
            var guid = new Guid(ShellIIDGuid.IEnumUnknown);

            var hr = NativeSearchCondition.GetSubConditions(ref guid, out var subConditionObj);

            if (!CoreErrorHelper.Succeeded(hr))
            {
                throw new ShellException(hr);
            }

            // Convert each ICondition to SearchCondition
            if (subConditionObj != null)
            {
                var enumUnknown = subConditionObj as IEnumUnknown;

                var buffer = IntPtr.Zero;
                uint fetched = 0;

                while (hr == HResult.Ok)
                {
                    hr = enumUnknown.Next(1, ref buffer, ref fetched);

                    if (hr == HResult.Ok && fetched == 1)
                    {
                        subConditionsList.Add(new SearchCondition((ICondition)Marshal.GetObjectForIUnknown(buffer)));
                    }
                }
            }

            return subConditionsList;
        }

        /// <summary>Release the native objects.</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (NativeSearchCondition != null)
            {
                Marshal.ReleaseComObject(NativeSearchCondition);
                NativeSearchCondition = null;
            }
        }
    }
}