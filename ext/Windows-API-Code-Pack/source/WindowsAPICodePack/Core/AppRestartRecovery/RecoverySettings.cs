//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// Defines methods and properties for recovery settings, and specifies options for an application that attempts to perform final actions
    /// after a fatal event, such as an unhandled exception.
    /// </summary>
    /// <remarks>This class is used to register for application recovery. See the <see cref="ApplicationRestartRecoveryManager"/> class.</remarks>
    public class RecoverySettings
    {
        private readonly uint pingInterval;
        private readonly RecoveryData recoveryData;

        /// <summary>Initializes a new instance of the <b>RecoverySettings</b> class.</summary>
        /// <param name="data">
        /// A recovery data object that contains the callback method (invoked by the system before Windows Error Reporting terminates the
        /// application) and an optional state object.
        /// </param>
        /// <param name="interval">
        /// The time interval within which the callback method must invoke
        /// <see cref="ApplicationRestartRecoveryManager.ApplicationRecoveryInProgress"/> to prevent WER from terminating the application.
        /// </param>
        /// <seealso cref="ApplicationRestartRecoveryManager"/>
        public RecoverySettings(RecoveryData data, uint interval)
        {
            recoveryData = data;
            pingInterval = interval;
        }

        /// <summary>
        /// Gets the recovery data object that contains the callback method and an optional parameter (usually the state of the application)
        /// to be passed to the callback method.
        /// </summary>
        /// <value>A <see cref="RecoveryData"/> object.</value>

        /* Unmerged change from project 'Core (netcoreapp3.0)'
        Before:
                public RecoveryData RecoveryData
                {
                    get { return recoveryData; }
        After:
                public RecoveryData RecoveryData => recoveryData; }
        */

        /* Unmerged change from project 'Core (net462)'
        Before:
                public RecoveryData RecoveryData
                {
                    get { return recoveryData; }
        After:
                public RecoveryData RecoveryData => recoveryData; }
        */

        /* Unmerged change from project 'Core (net472)'
        Before:
                public RecoveryData RecoveryData
                {
                    get { return recoveryData; }
        After:
                public RecoveryData RecoveryData => recoveryData; }
        */
        public uint PingInterval => pingInterval;
        public RecoveryData RecoveryData => recoveryData;

        /// <summary>
        /// Gets the time interval for notifying Windows Error Reporting. The <see cref="RecoveryCallback"/> method must invoke
        /// <see cref="ApplicationRestartRecoveryManager.ApplicationRecoveryInProgress"/> within this interval to prevent WER from
        /// terminating the application.
        /// </summary>
        /// <remarks>
        /// The recovery ping interval is specified in milliseconds. By default, the interval is 5 seconds. If you specify zero, the default
        /// interval is used.
        /// </remarks>

        /* Unmerged change from project 'Core (netcoreapp3.0)'
        Before:
                public uint PingInterval { get { return pingInterval; } }
        After:
                public uint PingInterval => pingInterval; } }
        */

        /* Unmerged change from project 'Core (net462)'
        Before:
                public uint PingInterval { get { return pingInterval; } }
        After:
                public uint PingInterval => pingInterval; } }
        */

        /* Unmerged change from project 'Core (net472)'
        Before:
                public uint PingInterval { get { return pingInterval; } }
        After:
                public uint PingInterval => pingInterval; } }
        */

        /// <summary>Returns a string representation of the current state of this object.</summary>
        /// <returns>A <see cref="System.String"/> object.</returns>
        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LocalizedMessages.RecoverySettingsFormatString,
                recoveryData.Callback.Method.ToString(),
                recoveryData.State.ToString(),
                PingInterval);
    }
}