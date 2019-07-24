/*
The MIT License(MIT)
Copyright (C) December 2018 devMobile Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace devMobile.Windows10IoTCore.GroveBaseHatRPI
{
	public class AnalogPorts : IDisposable
	{
		private const int I2CAddress = 0x04;
		private const byte RegisterDeviceId = 0x0;
		private const byte RegisterVersion = 0x02;
		private const byte RegisterPowerSupplyVoltage = 0x29;
		private const byte RegisterRawBase = 0x10;
		private const byte RegisterVoltageBase = 0x20;
		private const byte RegisterValueBase = 0x30;
		private const byte DeviceId = 0x0004;
		private I2cDevice Device= null;
		private bool Disposed = false;

		public enum AnalogPort
		{
			A0 = 0,
			A1 = 1,
			A2 = 2,
			A3 = 3,
			A4 = 4,
			A5 = 5,
			A6 = 6,
			A7 = 7,
			A8 = 8
		};

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.Disposed)
			{
				if (disposing)
				{
					if (Device != null)
					{
						Device.Dispose();
						Device = null;
					}
				}

				this.Disposed = true;
			}
		}

		~AnalogPorts()
		{
			Dispose(false);
		}

		public void Initialise(I2cBusSpeed i2CBusSpeed = I2cBusSpeed.StandardMode, I2cSharingMode i2CSharingMode = I2cSharingMode.Shared)
		{
			string aqs = I2cDevice.GetDeviceSelector();

			DeviceInformationCollection I2CBusControllers = DeviceInformation.FindAllAsync(aqs).AsTask().Result;
			if (I2CBusControllers.Count != 1)
			{
				throw new IndexOutOfRangeException("I2CBusControllers");
			}

			I2cConnectionSettings settings = new I2cConnectionSettings(I2CAddress)
			{
				BusSpeed = i2CBusSpeed,
				SharingMode = i2CSharingMode,
			};

			Device = I2cDevice.FromIdAsync(I2CBusControllers[0].Id, settings).AsTask().Result;

			byte[] writeBuffer = new byte[1] { RegisterDeviceId };
			byte[] readBuffer = new byte[1] { 0 };

			Device.WriteRead(writeBuffer, readBuffer);
			byte deviceId = readBuffer[0];
			Debug.WriteLine($"GroveBaseHatRPI DeviceId 0x{deviceId:X}");
			if (deviceId != DeviceId)
			{
				throw new Exception("GroveBaseHatRPI not found");
			}
		}

		public byte Version()
		{
			byte[] writeBuffer = new byte[1] { RegisterVersion };
			byte[] readBuffer = new byte[1] { 0 };
			Debug.Assert(Device != null, "Initialise method not called");

			Device.WriteRead(writeBuffer, readBuffer);
			byte version = readBuffer[0];

			Debug.WriteLine($"GroveBaseHatRPI version 0x{version:X}");

			return version;
		}

		public double PowerSupplyVoltage()
		{
			byte[] writeBuffer = new byte[1] { RegisterPowerSupplyVoltage };
			byte[] readBuffer = new byte[2] { 0, 0 };
			Debug.Assert(Device != null, "Initialise method not called");

			Device.WriteRead(writeBuffer, readBuffer);
			ushort value = BitConverter.ToUInt16(readBuffer, 0);

			Debug.WriteLine($"GroveBaseHatRPI PowerSupplyVoltage MSB 0x{readBuffer[1]:X} LSB 0x{readBuffer[0]:X} Value {value}");

			return value / 1000.0 ;
		}

		public ushort ReadRaw(AnalogPort analogPort)
		{
			byte register = (byte)analogPort;
			register += RegisterRawBase;
			byte[] writeBuffer = new byte[1] { register };
			byte[] readBuffer = new byte[2] { 0, 0 };
			Debug.Assert(Device != null, "Initialise method not called");

			Device.WriteRead(writeBuffer, readBuffer);
			ushort value = BitConverter.ToUInt16(readBuffer, 0);

			Debug.WriteLine($"GroveBaseHatRPI ReadRaw {analogPort} MSB 0x{readBuffer[1]:X} LSB 0x{readBuffer[0]:X} Value {value}");

			return value;
		}

		public double ReadVoltage(AnalogPort analogPort)
		{
			byte register = (byte)analogPort;
			register += RegisterVoltageBase;
			byte[] writeBuffer = new byte[1] { register };
			byte[] readBuffer = new byte[2] { 0, 0 };
			Debug.Assert(Device != null, "Initialise method not called");

			Device.WriteRead(writeBuffer, readBuffer);
			ushort value = BitConverter.ToUInt16(readBuffer, 0);

			Debug.WriteLine($"GroveBaseHatRPI ReadVoltage {analogPort} MSB 0x{readBuffer[1]:X} LSB 0x{readBuffer[0]:X} Value {value}");

			return value / 1000.0 ;
		}

		public double Read(AnalogPort analogPort)
		{
			byte register = (byte)analogPort;
			register += RegisterValueBase;
			byte[] writeBuffer = new byte[1] { register } ;
			byte[] readBuffer = new byte[2] { 0, 0 };
			Debug.Assert(Device != null, "Initialise method not called");

			Device.WriteRead(writeBuffer, readBuffer);
			ushort value = BitConverter.ToUInt16(readBuffer, 0);

			Debug.WriteLine($"GroveBaseHatRPI Read {analogPort} MSB 0x{readBuffer[1]:X} LSB 0x{readBuffer[0]:X} Value {value}");

			return (double)value / 10.0;
		}
	}
}
