/*
    Copyright ® Feb 2019 Aware Group & Microsoft, All Rights Reserved
 
    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE
*/
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Diagnostics;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;

using FarmBeatsLabs.UWP.Headless.Models;
using FarmBeatsLabs.UWP.Headless.Services;

using devMobile.Windows10IoTCore.GroveBaseHatRPI;
using Glovebox.IoT.Devices.Sensors;


namespace FarmBeatsLabs.UWP.Headless
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral backgroundTaskDeferral = null;
        private DeviceClient azureIoTHubClient = null;
        private Twin deviceTwin = null;

        private Timer sensorUpdateTimer;
        private readonly TimeSpan SensorUpdateDuePeriod = new TimeSpan(0, 0, 10);
        private AnalogPorts GroveBaseHatAnalogPorts;
        private BME280 bme280Sensor;

        private Timer ImageUpdatetimer;
        private readonly TimeSpan ImageUpdateDueDefault = new TimeSpan(0, 0, 15);
        private MediaCapture mediaCapture;

        private readonly TimeSpan DeviceRestartPeriod = new TimeSpan(0, 0, 25);

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var deviceId = "";
            var deviceSettings = default(AutodiscoverResult);
            CameraStatus cameraStatus = CameraStatus.NotInstalled;
            GroveBaseHatStatus groveBaseHatStatus = GroveBaseHatStatus.NotInstalled;

            #region Set up device autodiscovery
            //keep autodiscovering until we get a connection string
            while (string.IsNullOrEmpty(deviceSettings?.ConnectionString) == true)
            {
                try
                {
                    deviceId = DeviceIdService.GetDeviceId();
                    deviceSettings = AutodiscoverService.GetDeviceSettings(deviceId);

                    LoggingService.Log($"Device settings retrieved for DeviceId:{deviceId}");
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Autodiscovery failed", ex);
                    Thread.Sleep(30000);
                }
            }
            #endregion

            #region Set up IoT Hub Connection
            try
            {
                azureIoTHubClient = DeviceClient.CreateFromConnectionString(deviceSettings.ConnectionString, TransportType.Mqtt);
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Azure IoT Hub connection failed", ex);
                //TODO: set up a way for this to retry instead of fail?
                return;
            }
            #endregion

            #region Report device startup
            SendPayloadToIoTHub(new DeviceEvent(){ApplicationStarted = true});
            #endregion

            #region Retrieve device twin
            try
            {
                deviceTwin = azureIoTHubClient.GetTwinAsync().Result;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Azure IoT Hub device twin configuration retrieval failed", ex);
                //TODO: set up a way for this to retry instead of fail?
                return;
            }
            #endregion

            #region Report device properties
            try
            {
                TwinCollection reportedProperties;
                reportedProperties = new TwinCollection();

                // This is from the OS 
                reportedProperties["Timezone"] = TimeZoneSettings.CurrentTimeZoneDisplayName;
                reportedProperties["OSVersion"] = Environment.OSVersion.VersionString;
                reportedProperties["MachineName"] = Environment.MachineName;

                // This is from the application manifest 
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version; 
                reportedProperties["ApplicationDisplayName"] = package.DisplayName;
                reportedProperties["ApplicationName"] = packageId.Name;
                reportedProperties["ApplicationVersion"] = string.Format($"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");

                // Unique identifier from the hardware
                SystemIdentificationInfo systemIdentificationInfo = SystemIdentification.GetSystemIdForPublisher();
                using (DataReader reader = DataReader.FromBuffer(systemIdentificationInfo.Id))
                {
                    byte[] bytes = new byte[systemIdentificationInfo.Id.Length];
                    reader.ReadBytes(bytes);
                    reportedProperties["SystemId"] = BitConverter.ToString(bytes);
                }

                azureIoTHubClient.UpdateReportedPropertiesAsync(reportedProperties).Wait();
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Azure IoT Hub device twin configuration retrieval failed", ex);
                //TODO: set up a way for this to retry instead of fail?
                return;
            }
            #endregion

            #region Wire up device reboot command handler
            try
            {
                azureIoTHubClient.SetMethodHandlerAsync("Restart", RestartAsync, null);
            }
            catch( Exception ex)
            {
                LoggingService.Error($"Azure IoT Hub device method handler configuration failed", ex);
                return;
            }
            #endregion

            #region Set up Grove Base Hat for RPI if installed
            if (deviceTwin.Properties.Desired.Contains("GroveBaseHatForRPIInstalled"))
            {
                groveBaseHatStatus = GroveBaseHatStatus.Installed;

                try
                {
                    if (deviceTwin.Properties.Desired["GroveBaseHatForRPIInstalled"].value == true)
                    {
                        GroveBaseHatAnalogPorts = new AnalogPorts();
                        GroveBaseHatAnalogPorts.Initialise();
                        LoggingService.Log($"Grove Base Hat for RPI Firmware version:{GroveBaseHatAnalogPorts.Version()}");

                        bme280Sensor = new BME280(0x76);

                        // Wire up the sensor update handler
                        azureIoTHubClient.SetMethodHandlerAsync("SensorUpdate", SensorUpdateAsync, deviceId);

                        // If the SensorUpdatePeriod greater than 0 start timer
                        int sensorUpdatePeriod = deviceTwin.Properties.Desired["SensorUpdatePeriod"].value;
                        LoggingService.Log($"Sensor update period:{sensorUpdatePeriod} seconds");
                        if (sensorUpdatePeriod > 0)
                        {
                            sensorUpdateTimer = new Timer(SensorUpdateTimerCallback, deviceId, SensorUpdateDuePeriod, new TimeSpan(0, 0, sensorUpdatePeriod));
                            groveBaseHatStatus = GroveBaseHatStatus.Automatic;
                        }
                        else
                        {
                            groveBaseHatStatus = GroveBaseHatStatus.Enabled;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Grove Base Hat for RPI sensor configuration failed", ex);
                    return;
                }
            }
            SendPayloadToIoTHub(new DeviceStatus() { GroveBaseHatStatus = groveBaseHatStatus });
            #endregion

            #region Set up Camera if installed
            if (deviceTwin.Properties.Desired.Contains("CameraInstalled"))
            {
                cameraStatus = CameraStatus.Installed;

                try
                {
                    if (deviceTwin.Properties.Desired["CameraInstalled"].value == true)
                    {
                        mediaCapture = new MediaCapture();
                        mediaCapture.InitializeAsync().AsTask().Wait();

                        // Wire up the image update handler
                        azureIoTHubClient.SetMethodHandlerAsync("ImageUpdate", ImageUpdateAsync, deviceId);

                        // If the CameraUpdatePeriod greater than 0 start timer
                        int imageUpdatePeriod = deviceTwin.Properties.Desired["ImageUpdatePeriod"].value;
                        LoggingService.Log($"Image update period:{imageUpdatePeriod} seconds");
                        if (imageUpdatePeriod > 0)
                        {
                            ImageUpdatetimer = new Timer(ImageUpdateTimerCallback, deviceId, ImageUpdateDueDefault, new TimeSpan(0, 0, imageUpdatePeriod));
                            cameraStatus = CameraStatus.Automatic;
                        }
                        else
                        {
                            cameraStatus = CameraStatus.Enabled;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Image capture configuration failed", ex);
                }
            }
            SendPayloadToIoTHub(new DeviceStatus() { CameraStatus = cameraStatus });
            #endregion

            //enable task to continue running in background
            backgroundTaskDeferral = taskInstance.GetDeferral();
        }

        private void SensorUpdateTimerCallback(object state)
        {
            string deviceId = (string)state;

            SensorValueupload(deviceId);
        }

        private async Task<MethodResponse> SensorUpdateAsync(MethodRequest methodRequest, object userContext)
        {
            string deviceId = (string)userContext;

            SensorValueupload(deviceId);

            SendPayloadToIoTHub(new DeviceEvent(){SensorUpdateManual = true});

            return new MethodResponse(200);
        }

        private void SensorValueupload(object state)
        {
            try
            {
                // Air sensor readings
                double temperatureCelsius = bme280Sensor.Temperature.DegreesCelsius;
                double temperatureFahrenheit = bme280Sensor.Temperature.DegreesFahrenheit;
                double humidity = bme280Sensor.Humidity;
                double pressure = bme280Sensor.Pressure.Kilopascals;

                double lightLevel = GroveBaseHatAnalogPorts.Read( AnalogPorts.AnalogPort.A0);
                double soilMoisture1 = GroveBaseHatAnalogPorts.Read(AnalogPorts.AnalogPort.A2);
                double soilMoisture2 = GroveBaseHatAnalogPorts.Read(AnalogPorts.AnalogPort.A4);

                LoggingService.Log(string.Format("C {0:0.0}° F {1:0.0}° H {2:0}% P {3:0.000}kPa L {4:0}%  Soil1 {5:0}% Soil2 {6:0}% ", temperatureCelsius, temperatureFahrenheit, humidity, pressure, lightLevel, soilMoisture1, soilMoisture2));

                // Setup for the the logging of sensor values
                var loggingData = new LoggingFields();

                // Construct Azure IoT Central friendly payload
                var telemetryDataPoint = new TelemetryDataPoint();
                telemetryDataPoint.Celsius = temperatureCelsius;
                loggingData.AddDouble(nameof(telemetryDataPoint.Celsius), temperatureCelsius);
                telemetryDataPoint.Fahrenheit = temperatureFahrenheit;
                loggingData.AddDouble(nameof(telemetryDataPoint.Fahrenheit), temperatureFahrenheit);
                telemetryDataPoint.Humidity = humidity;
                loggingData.AddDouble(nameof(telemetryDataPoint.Humidity), humidity);
                telemetryDataPoint.Pressure = pressure;
                loggingData.AddDouble(nameof(telemetryDataPoint.Pressure), pressure);

                telemetryDataPoint.Light = lightLevel;
                loggingData.AddDouble(nameof(telemetryDataPoint.Light), lightLevel);

                telemetryDataPoint.SoilMoisture1 = soilMoisture1;
                loggingData.AddDouble(nameof(telemetryDataPoint.SoilMoisture1), soilMoisture1);
                telemetryDataPoint.SoilMoisture2 = soilMoisture2;
                loggingData.AddDouble(nameof(telemetryDataPoint.SoilMoisture2), soilMoisture2);

                // Log the sensor values to ETW logging
                LoggingService.LogEvent("Sensor readings", loggingData, LoggingLevel.Verbose);

                using (var message = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryDataPoint))))
                {
                    LoggingService.Log("AzureIoTHubClient SendEventAsync starting");
                    azureIoTHubClient.SendEventAsync(message).Wait();
                    LoggingService.Log("AzureIoTHubClient SendEventAsync finished");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to send telemetry", ex);
            }
        }

        private void ImageUpdateTimerCallback(object state)
        {
            string deviceId = (string)state;

            ImageUpload(deviceId);
        }

        private async Task<MethodResponse> ImageUpdateAsync(MethodRequest methodRequest, object userContext)
        {
            string deviceId = (string)userContext;

            ImageUpload(deviceId);
  
            SendPayloadToIoTHub(new DeviceEvent(){ImageUpdateManual = true});

            return new MethodResponse(200);
        }

        private async void ImageUpload( string deviceId)
        {
            try
            {
                using (Windows.Storage.Streams.InMemoryRandomAccessStream captureStream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                {
                    await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);
                    await captureStream.FlushAsync();
                    captureStream.Seek(0);

                    // Drops file onto device file system for debugging only
#if DEBUG
                    IStorageFile photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync( "Timelapse.jpg", CreationCollisionOption.ReplaceExisting);
                    ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                    await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
#endif
                    LoggingService.Log("ImageUploadService Upload starting");
                    ImageUploadService.Upload(deviceId, captureStream);
                    LoggingService.Log("ImageUploadService Upload done");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Image capture or upload failed ", ex);
            }
        }

        private async Task<MethodResponse> RestartAsync(MethodRequest methodRequest, object userContext)
        {
            LoggingService.Log("Restart initiated");
            // Stop the camera and sensor timers (if running) before reboot
            if (sensorUpdateTimer != null)
            {
                sensorUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (ImageUpdatetimer!=null)
            {
                ImageUpdatetimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            SendPayloadToIoTHub(new DeviceEvent(){DeviceRestartManual = true});

            ShutdownManager.BeginShutdown(ShutdownKind.Restart, DeviceRestartPeriod);

            return new MethodResponse(200);
        }

        private void SendPayloadToIoTHub( object payload)
        {
            try
            {
                using (Message message = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(payload))))
                {
                    LoggingService.Log($"AzureIoTHubClient SendPayloadToIoTHub starting {payload.GetType()}");
                    azureIoTHubClient.SendEventAsync(message).Wait();
                    LoggingService.Log($"AzureIoTHubClient SendPayloadToIoTHub finished {payload.GetType()}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"AzureIoTHubClient SendEventAsync failed {payload.GetType()}", ex);
            }
        }
    }
}
