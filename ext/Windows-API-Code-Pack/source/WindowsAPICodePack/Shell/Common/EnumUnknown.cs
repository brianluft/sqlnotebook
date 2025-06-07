//Copyright (c) Microsoft Corporation.  All rights reserved.

using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Shell
{
    internal class EnumUnknownClass : IEnumUnknown
    {
        private readonly List<ICondition> conditionList = new List<ICondition>();
        private int current = -1;

        internal EnumUnknownClass(ICondition[] conditions) => conditionList.AddRange(conditions);

        public HResult Clone(out IEnumUnknown result)
        {
            result = new EnumUnknownClass(conditionList.ToArray());
            return HResult.Ok;
        }

        public HResult Next(uint requestedNumber, ref IntPtr buffer, ref uint fetchedNumber)
        {
            current++;

            if (current < conditionList.Count)
            {
                buffer = Marshal.GetIUnknownForObject(conditionList[current]);
                fetchedNumber = 1;
                return HResult.Ok;
            }

            return HResult.False;
        }

        public HResult Reset()
        {
            current = -1;
            return HResult.Ok;
        }

        public HResult Skip(uint number)
        {
            var temp = current + (int)number;

            if (temp > (conditionList.Count - 1))
            {
                return HResult.False;
            }

            current = temp;
            return HResult.Ok;
        }
    }
}