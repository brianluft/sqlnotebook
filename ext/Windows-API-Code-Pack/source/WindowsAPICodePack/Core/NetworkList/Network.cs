//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Net
{
    /// <summary>
    /// Represents a network on the local machine. It can also represent a collection of network connections with a similar network signature.
    /// </summary>
    /// <remarks>Instances of this class are obtained by calling methods on the <see cref="NetworkListManager"/> class.</remarks>
    public class Network
    {
        private readonly INetwork network;

        internal Network(INetwork network) => this.network = network;

        /// <summary>Gets or sets the category of a network. The categories are trusted, untrusted, or authenticated.</summary>
        /// <value>A <see cref="NetworkCategory"/> value.</value>
        public NetworkCategory Category
        {
            get => network.GetCategory();

            set => network.SetCategory(value);
        }

        /// <summary>Gets the local date and time when the network was connected.</summary>
        /// <value>A <see cref="System.DateTime"/> object.</value>
        public DateTime ConnectedTime
        {
            get
            {
                network.GetTimeCreatedAndConnected(out var dummy1, out var dummy2, out var low, out var high);
                long time = high;
                // Shift the day info into the high order bits.
                time <<= 32;
                time |= low;
                return DateTime.FromFileTimeUtc(time);
            }
        }

        /// <summary>Gets the network connections for the network.</summary>
        /// <value>A <see cref="NetworkConnectionCollection"/> object.</value>
        public NetworkConnectionCollection Connections => new NetworkConnectionCollection(network.GetNetworkConnections());

        /// <summary>Gets the connectivity state of the network.</summary>
        /// <value>A <see cref="Connectivity"/> value.</value>
        /// <remarks>Connectivity provides information on whether the network is connected, and the protocols in use for network traffic.</remarks>
        public ConnectivityStates Connectivity => network.GetConnectivity();

        /// <summary>Gets the local date and time when the network was created.</summary>
        /// <value>A <see cref="System.DateTime"/> object.</value>
        public DateTime CreatedTime
        {
            get
            {
                network.GetTimeCreatedAndConnected(out var low, out var high, out var dummy1, out var dummy2);
                long time = high;
                //Shift the value into the high order bits.
                time <<= 32;
                time |= low;
                return DateTime.FromFileTimeUtc(time);
            }
        }

        /// <summary>Gets or sets a description for the network.</summary>
        /// <value>A <see cref="System.String"/> value.</value>
        public string Description
        {
            get => network.GetDescription();

            set => network.SetDescription(value);
        }

        /// <summary>Gets the domain type of the network.</summary>
        /// <value>A <see cref="DomainType"/> value.</value>
        /// <remarks>
        /// The domain indictates whether the network is an Active Directory Network, and whether the machine has been authenticated by
        /// Active Directory.
        /// </remarks>
        public DomainType DomainType => network.GetDomainType();

        /// <summary>Gets or sets the name of the network.</summary>
        /// <value>A <see cref="System.String"/> value.</value>
        public string Name
        {
            get => network.GetName();

            set => network.SetName(value);
        }

        /// <summary>Gets a unique identifier for the network.</summary>
        /// <value>A <see cref="System.Guid"/> value.</value>
        public Guid NetworkId => network.GetNetworkId();

        /// <summary>Gets a value that indicates whether there is network connectivity.</summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public bool IsConnected => network.IsConnected;

        /// <summary>Gets a value that indicates whether there is Internet connectivity.</summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public bool IsConnectedToInternet => network.IsConnectedToInternet;
    }
}