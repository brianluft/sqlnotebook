// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.WindowsAPICodePack.Sensors.Resources;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>Represents the method that will handle the system sensor list change.</summary>
    /// <param name="e">The event data.</param>
    public delegate void SensorsChangedEventHandler(SensorsChangedEventArgs e);

    /// <summary>Specifies the types of change in sensor availability.</summary>
    public enum SensorAvailabilityChange
    {
        /// <summary>A sensor has been added.</summary>
        Addition,

        /// <summary>A sensor has been removed.</summary>
        Removal
    }

    /// <summary>Data associated with a sensor type GUID.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SensorTypeData
    {
        private readonly Type sensorType;
        private readonly SensorDescriptionAttribute sda;

        public SensorTypeData(Type sensorClassType, SensorDescriptionAttribute sda)
        {
            sensorType = sensorClassType;
            this.sda = sda;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SensorDescriptionAttribute Attr => sda;

        public Type SensorType => sensorType;
    }

    /// <summary>Manages the sensors conected to the system.</summary>
    public static class SensorManager
    {
        /// <summary>Sensor type GUID -&gt; .NET Type + report type</summary>
        private static readonly Dictionary<Guid, SensorTypeData> guidToSensorDescr = new Dictionary<Guid, SensorTypeData>();

        private static readonly NativeISensorManager sensorManager = new NativeSensorManager();

        private static readonly nativeSensorManagerEventSink sensorManagerEventSink = new nativeSensorManagerEventSink();

        /// <summary>.NET type -&gt; type GUID.</summary>
        private static readonly Dictionary<Type, Guid> sensorTypeToGuid = new Dictionary<Type, Guid>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static SensorManager()
        {
            CoreHelpers.ThrowIfNotWin7();

            BuildSensorTypeMap();
            Thread.MemoryBarrier();
            sensorManager.SetEventSink(sensorManagerEventSink);
        }

        /// <summary>Occurs when the system's list of sensors changes.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly",
            Justification = "The event is raised from a static method, and so providing the instance of the sender is not possible")]
        public static event SensorsChangedEventHandler SensorsChanged;

        /// <summary>Retireves a collection of all sensors.</summary>
        /// <returns>A list of all sensors.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static SensorList<Sensor> GetAllSensors() => GetSensorsByCategoryId(SensorCategories.All);

        /// <summary>Returns a specific sensor by sensor ID.</summary>
        /// <typeparam name="T">A strongly typed sensor.</typeparam>
        /// <param name="sensorId">The unique identifier of the sensor.</param>
        /// <returns>A particular sensor.</returns>
        public static T GetSensorBySensorId<T>(Guid sensorId) where T : Sensor
        {
            var hr = sensorManager.GetSensorByID(sensorId, out var nativeSensor);
            if (hr == HResult.ElementNotFound)
            {
                throw new SensorPlatformException(LocalizedMessages.SensorsNotFound);
            }

            if (nativeSensor != null)
            {
                return GetSensorWrapperInstance<T>(nativeSensor);
            }

            return null;
        }

        /// <summary>Retrieves a collection of sensors filtered by category ID.</summary>
        /// <param name="category">The category ID of the requested sensors.</param>
        /// <returns>A list of sensors of the specified category ID.</returns>
        public static SensorList<Sensor> GetSensorsByCategoryId(Guid category)
        {
            var hr = sensorManager.GetSensorsByCategory(category, out var sensorCollection);
            if (hr == HResult.ElementNotFound)
                throw new SensorPlatformException(LocalizedMessages.SensorsNotFound);

            return nativeSensorCollectionToSensorCollection<Sensor>(sensorCollection);
        }

        /// <summary>Returns a collection of sensors filtered by type ID.</summary>
        /// <param name="typeId">The type ID of the sensors requested.</param>
        /// <returns>A list of sensors of the spefified type ID.</returns>
        public static SensorList<Sensor> GetSensorsByTypeId(Guid typeId)
        {
            var hr = sensorManager.GetSensorsByType(typeId, out var sensorCollection);
            if (hr == HResult.ElementNotFound)
            {
                throw new SensorPlatformException(LocalizedMessages.SensorsNotFound);
            }
            return nativeSensorCollectionToSensorCollection<Sensor>(sensorCollection);
        }

        /// <summary>Returns a strongly typed collection of specific sensor types.</summary>
        /// <typeparam name="T">The type of the sensors to retrieve.</typeparam>
        /// <returns>A strongly types list of sensors.</returns>
        public static SensorList<T> GetSensorsByTypeId<T>() where T : Sensor
        {
            var attrs = typeof(T).GetCustomAttributes(typeof(SensorDescriptionAttribute), true);
            if (attrs != null && attrs.Length > 0)
            {
                var sda = attrs[0] as SensorDescriptionAttribute;

                var hr = sensorManager.GetSensorsByType(sda.SensorTypeGuid, out var nativeSensorCollection);
                if (hr == HResult.ElementNotFound)
                {
                    throw new SensorPlatformException(LocalizedMessages.SensorsNotFound);
                }
                return nativeSensorCollectionToSensorCollection<T>(nativeSensorCollection);
            }

            return new SensorList<T>();
        }

        /// <summary>Opens a system dialog box to request user permission to access sensor data.</summary>
        /// <param name="parentWindowHandle">The handle to a window that can act as a parent to the permissions dialog box.</param>
        /// <param name="modal">Specifies whether the window should be modal.</param>
        /// <param name="sensors">The sensors for which to request permission.</param>
        public static void RequestPermission(IntPtr parentWindowHandle, bool modal, SensorList<Sensor> sensors)
        {
            if (sensors == null || sensors.Count == 0)
            {
                throw new ArgumentException(LocalizedMessages.SensorManagerEmptySensorsCollection, "sensors");
            }

            ISensorCollection sensorCollection = new SensorCollection();

            foreach (var sensor in sensors)
            {
                sensorCollection.Add(sensor.internalObject);
            }

            sensorManager.RequestPermissions(parentWindowHandle, sensorCollection, modal);
        }

        internal static SensorList<S> nativeSensorCollectionToSensorCollection<S>(ISensorCollection nativeCollection) where S : Sensor
        {
            var sensors = new SensorList<S>();

            if (nativeCollection != null)
            {
                nativeCollection.GetCount(out var sensorCount);

                for (uint i = 0; i < sensorCount; i++)
                {
                    nativeCollection.GetAt(i, out var iSensor);
                    var sensor = GetSensorWrapperInstance<S>(iSensor);
                    if (sensor != null)
                    {
                        sensor.internalObject = iSensor;
                        sensors.Add(sensor);
                    }
                }
            }

            return sensors;
        }

        /// <summary>Notification that the list of sensors has changed</summary>
        internal static void OnSensorsChanged(Guid sensorId, SensorAvailabilityChange change)
        {
            if (SensorsChanged != null)
            {
                SensorsChanged.Invoke(new SensorsChangedEventArgs(sensorId, change));
            }
        }

        /// <summary>
        /// Interrogates assemblies currently loaded into the AppDomain for classes deriving from <see cref="Sensor"/>. Builds data
        /// structures which map those types to sensor type GUIDs.
        /// </summary>
        private static void BuildSensorTypeMap()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in loadedAssemblies)
            {
                try
                {
                    var exportedTypes = asm.GetExportedTypes();
                    foreach (var t in exportedTypes)
                    {
                        if (t.IsSubclassOf(typeof(Sensor)) && t.IsPublic && !t.IsAbstract && !t.IsGenericType)
                        {
                            var attrs = t.GetCustomAttributes(typeof(SensorDescriptionAttribute), true);
                            if (attrs != null && attrs.Length > 0)
                            {
                                var sda = (SensorDescriptionAttribute)attrs[0];
                                var stm = new SensorTypeData(t, sda);

                                guidToSensorDescr.Add(sda.SensorTypeGuid, stm);
                                sensorTypeToGuid.Add(t, sda.SensorTypeGuid);
                            }
                        }
                    }
                }
                catch (System.NotSupportedException)
                {
                    // GetExportedTypes can throw this if dynamic assemblies are loaded via Reflection.Emit.
                }
                catch (System.IO.FileNotFoundException)
                {
                    // GetExportedTypes can throw this if a loaded asembly is not in the current directory or path.
                }
            }
        }

        /// <summary>
        /// Returns an instance of a sensor wrapper appropritate for the given sensor COM interface. If no appropriate sensor wrapper type
        /// could be found, the object created will be of the base-class type <see cref="Sensor"/>.
        /// </summary>
        /// <param name="nativeISensor">The underlying sensor COM interface.</param>
        /// <returns>A wrapper instance.</returns>
        private static S GetSensorWrapperInstance<S>(ISensor nativeISensor) where S : Sensor
        {
            nativeISensor.GetType(out var sensorTypeGuid);

            var sensorClassType =
                guidToSensorDescr.TryGetValue(sensorTypeGuid, out var stm) ? stm.SensorType : typeof(UnknownSensor);

            try
            {
                var sensor = (S)Activator.CreateInstance(sensorClassType);
                sensor.internalObject = nativeISensor;
                return sensor;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }
    }

    /// <summary>Defines the data passed to the SensorsChangedHandler.</summary>
    public class SensorsChangedEventArgs : EventArgs
    {
        internal SensorsChangedEventArgs(Guid sensorId, SensorAvailabilityChange change)
        {
            SensorId = sensorId;
            Change = change;
        }

        /// <summary>The type of change.</summary>
        public SensorAvailabilityChange Change
        {
            get;
            set;
        }

        /// <summary>The ID of the sensor that changed.</summary>
        public Guid SensorId
        {
            get;
            set;
        }
    }

    internal class nativeSensorManagerEventSink : ISensorManagerEvents
    {
        public void OnSensorEnter(ISensor nativeSensor, NativeSensorState state)
        {
            if (state == NativeSensorState.Ready)
            {
                var hr = nativeSensor.GetID(out var sensorId);
                if (hr == HResult.Ok)
                {
                    SensorManager.OnSensorsChanged(sensorId, SensorAvailabilityChange.Addition);
                }
            }
        }
    }
}