//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.WindowsAPICodePack.Net
{
    /// <summary>An enumerable collection of <see cref="Network"/> objects.</summary>
    public class NetworkCollection : IEnumerable<Network>
    {
        private readonly IEnumerable networkEnumerable;

        internal NetworkCollection(IEnumerable networkEnumerable) => this.networkEnumerable = networkEnumerable;

        /// <summary>Returns the strongly typed enumerator for this collection.</summary>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerator{T}"/> object.</returns>
        public IEnumerator<Network> GetEnumerator()
        {
            foreach (INetwork network in networkEnumerable)
            {
                yield return new Network(network);
            }
        }

        /// <summary>
        /// Returns the enumerator for this collection.
        /// </summary>
        ///<returns>An <see cref="System.Collections.IEnumerator"/> object.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (INetwork network in networkEnumerable)
            {
                yield return new Network(network);
            }
        }
    }
}