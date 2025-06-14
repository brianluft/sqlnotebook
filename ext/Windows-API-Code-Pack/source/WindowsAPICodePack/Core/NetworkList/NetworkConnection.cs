﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Net
{
    /// <summary>Represents a connection to a network.</summary>
    /// <remarks>
    /// A collection containing instances of this class is obtained by calling the
    /// <see cref="P:Microsoft.WindowsAPICodePack.Net.Network.Connections"/> property.
    /// </remarks>
    public class NetworkConnection
    {
        private readonly INetworkConnection networkConnection;

        internal NetworkConnection(INetworkConnection networkConnection) => this.networkConnection = networkConnection;

        /// <summary>Gets the adapter identifier for this connection.</summary>
        /// <value>A <see cref="System.Guid"/> object.</value>
        public Guid AdapterId => networkConnection.GetAdapterId();

        /// <summary>Gets the unique identifier for this connection.</summary>
        /// <value>A <see cref="System.Guid"/> object.</value>
        public Guid ConnectionId => networkConnection.GetConnectionId();

        /// <summary>Gets a value that indicates the connectivity of this connection.</summary>
        /// <value>A <see cref="Connectivity"/> value.</value>
        public ConnectivityStates Connectivity => networkConnection.GetConnectivity();

        /// <summary>
        /// Gets a value that indicates whether the network associated with this connection is an Active Directory network and whether the
        /// machine has been authenticated by Active Directory.
        /// </summary>
        /// <value>A <see cref="DomainType"/> value.</value>
        public DomainType DomainType => networkConnection.GetDomainType();

        /// <summary>Gets a value that indicates whether this connection has network connectivity.</summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public bool IsConnected => networkConnection.IsConnected;

        /// <summary>Gets a value that indicates whether this connection has Internet access.</summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public bool IsConnectedToInternet => networkConnection.IsConnectedToInternet;

        /// <summary>Retrieves an object that represents the network associated with this connection.</summary>
        /// <returns>A <see cref="Network"/> object.</returns>
        public Network Network => new Network(networkConnection.GetNetwork());
    }
}