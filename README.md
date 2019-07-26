# Farmbeats Labs GrovePI Gateway
## Overview
The Farmbeats labs GrovePI Gateway application is a Microsoft .Net Core  Universal Windows Platform (UWP) Windows 10 IoT Core  Background application . This application uploads data from SeeedStudio Grove sensors  attached to a Dexter Industries GrovePI  and images from an optional Web Camera .

The Grove sensor data is uploaded to a Microsoft Azure IoT Central  hosted Azure IoT Hub  for analysis and display. The web camera images are uploaded to Microsoft Azure Blob storage  for post processing and analysis.

## Development Tools

The application can be deployed to a Raspberry PI  device running Windows 10 IoT Core by downloading the latest installer from (https://github.com/?) or, by downloading the source code from (https://github.com/?) then building and deploying it using Microsoft Visual Studio 2017 .

To build and deploy the application Visual Studio 2017 needs the Windows 10 IoT Core Project Templates for VS 2017  and associated dependencies installed. The application was built using the Visual C# template, there are also templates for C/C++, JavaScript and Arduino Wiring projects.

The application uses the Azure IoT Hub device client library  (available via Nuget ) to keep the device settings and properties up to date, and upload telemetry data to the Azure IoT Hub.

The application reads values from the sensors attached to the GrovePi using the GrovePi NuGet package . Dexter industries have created sample applications which show how other SeeedStudio Grove sensors and actuators should be used .

The application uses the Newtonsoft Json.Net  framework (available via NuGet) for serialising message payloads.

## Application Structure
The application consists of a core file StartupTask.cs which contains the application logic, plus services for device provisioning, retrieving the device Mac Address , and uploading images. 
### Provisioning
On start-up the application calls the FarmBeats device provisioning service every 30 seconds until it successfully retrieves an Azure IoT Hub connection string.

### Device Twin 
Once the device is connected to an Azure IoT Hub, device settings are synchronised with the configuration stored in Azure IoT Central. The Device Twin functionality is only available for MQ Queue Telemetry Transport (MQTT) protocol connected devices (Sept 2018) so TransportType.Mqtt is required when creating the Azure IoT Hub connection. 

If any of the device twin settings are changed the application will need to be restarted for them to take effect.

### Device Properties
The device supports the following properties which are uploaded to the Azure IoT Hub every time the application starts. These used in the provisioning process and to help with debugging.

Timezone – configured via Device Portal so both UTC /local times are consistent
*	OSVersion – Version of the Windows 10 IoT Core operating system installed on the device
*	MachineName – Network name of the device
*	ApplicationDisplayName –User friendly name of the application
*	ApplicationName – The publisher name of the application
*	ApplicationVersion – Version of the application
*	SystemID – Publisher specific unique device identifier used for debugging/provisioning.

### Application Commands
The application supports three commands, which can be initiated from Azure IoT Central. The execution of each command is also tracked with an event.

#### Sensor Update
Initiates an out of sequence sensor data update. (Useful for checking the installation/configuration of sensors)

#### Image Update
Takes an out of sequence image and uploads it to Azure storage. (Useful for aligning the camera)

#### Restart Device
The application forces the device to restart after stopping the Sensor and Camera update timers (this also forces device twin settings to be updated and updates to be applied)
 
### Application Events
The application notifies the Azure IoT hub of four different events

#### Sensor Update Manual
An out of sequence telemetry data update has just completed

#### Image Update Manual
An out of sequence image has just been uploaded

#### Device Restart Manual
A device restart has just been initiated

#### Application Started
The application has just started. This could have been initiated by a user or the device restarted automatically after applying updates.

### Grove PI Configuration
The GrovePI has two device twin settings
*	GrovePIInstalled – A Boolean flag indicating whether the GrovePI should be configured.
*	SensorUpdatePeriod – The period for sensor data updates in seconds. 

If the SensorUpdatePeriod is 0 the GrovePI is configured but the update timer is not started. The GrovePI firmware version is logged to help with debugging.

### Camera Configuration
The Camera has two device twin settings
*	CameraInstalled – A Boolean flag indicating whether a Camera should be configured.
*	ImageUpdatePeriod - The period for camera image updates in seconds. 

If the ImageUpdatePeriod is 0 the Camera is configured but the update timer is not started.

### Sensor Value Upload
The telemetry data point Centigrade, Fahrenheit & Humidity values are nullable because the GrovePI Sensirion  DHT22 driver randomly returns Not a Number(NaN ). 

The soil moisture, and light level analog inputs also every so often returns zero  but as zero is a valid value it was difficult to identify incorrect values.
~~~~
public sealed class TelemetryDataPoint
{
  [JsonProperty("Centigrade", NullValueHandling = NullValueHandling.Ignore)]
  public double? Centigrade {get; set;}
  [JsonProperty("Fahrenheit", NullValueHandling = NullValueHandling.Ignore)]
  public double? Fahrenheit {get; set;}
  [JsonProperty("Humidity", NullValueHandling = NullValueHandling.Ignore)]
  public double? Humidity {get; set;}
  public double Light {get; set;}
  public double SoilMoisture1 {get; set;}
  public double SoilMoisture2 {get; set;}
}
~~~~

### Image Upload
The application uploads images to an HTTPS endpoint as a stream of bytes. When built in debugging mode the application also stores a copy of the most recent image (timelapse.jpg) in “User Folders\Pictures” on the device.

### Application Status
The application reports two status values, one for the camera the other for the GrovePI. These can be used to track the progress of the application as it starts.
~~~~
public enum CameraStatus
{
   Undefined = 0,
   NotInstalled,
   Installed,
   Enabled,
   Automatic
}

public enum GrovePiStatus
{
   Undefined = 0,
   NotInstalled,
   Installed,
   Enabled,
   Automatic
}
~~~~


In Azure IoT Central these enumeration vales are mapped to user friendly text labels and colours.

## Extension Projects
### Soil Moisture Sensor Evaluation
The Grove Soil Moisture sensors are designed for short-term indoor use and will degrade over time. Trialling other sensors and technologies for longer-term and/or outdoor use could be a standalone project

This capacitive soil moisture sensor is plug compatible with the existing analog input
https://makerfabs.com/index.php?route=product/product&product_id=389

This I2C Soil Moisture Sensor is available in indoor and outdoor versions and would free up an analog port for another sensor.
https://www.tindie.com/products/miceuz/i2c-soil-moisture-sensor/

This SoilWatch 10 -Soil Sensor is an analog sensor designed for outdoors use (3V version)
https://www.tindie.com/products/pinotech/soilwatch-10-soil-moisture-sensor/

This Soil Temperature and Humidity Sensor is designed for long-term outdoors use and uses the same GrovePI driver as the temperature and humidity sensor in the kit.
https://makerfabs.com/index.php?route=product/product&product_id=384

This I2C Sunlight Sensor could be used to monitor light levels and would free up a port for another analog sensor.
https://www.seeedstudio.com/Grove-Digital-Light-Sensor-p-1281.html

This I2C UV Light  Sensor could be used to monitor UV levels and would free up a port for another analog sensor.
https://www.seeedstudio.com/Grove-I2C-UV-Sensor-VEML607-p-3195.html

For sensors with unterminated cables these could be used
https://www.seeedstudio.com/Grove-Screw-Terminal-p-996.html

### Automatic Watering
The software on the device could be extended to control a pump and measure the amount of water used.

385 DC Diaphragm Pump
https://makerfabs.com/index.php?route=product/product&product_id=95

Grove Relay
https://www.seeedstudio.com/Grove-Relay-p-769.html

1/8” Water Flow Sensor
https://www.seeedstudio.com/G1-8-Water-Flow-Sensor-p-1346.html

## Summary
The Farmbeats Labs Student Kit is intended to provide an introduction to Precision Agriculture with a selection of environment sensors. 

This project is just the beginning and over the next couple of months we will be releasing additional  functionality and working with students from all over the world to explore how the Internet of Things (IoT) can improve agriculture and the environment. 

Share your ideas and projects with others at https://github.com/farmbeatslabs/
