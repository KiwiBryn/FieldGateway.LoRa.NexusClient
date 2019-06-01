// <copyright file="client.cs" company="devMobile Software">
// Copyright ® 2019 Feb devMobile Software, All Rights Reserved
//
//  MIT License
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE"
//
// </copyright>
namespace devMobile.IoT.Nexus.FieldGateway
{
	using System;
	using System.Text;
	using System.Threading;
	using Microsoft.SPOT;
	using Microsoft.SPOT.Hardware;

	using devMobile.IoT.NetMF.ISM;
	using devMobile.NetMF.Sensor;
	using IngenuityMicro.Nexus;

	class NexusClient
	{
		Rfm9XDevice rfm9XDevice;
		private readonly TimeSpan dueTime = new TimeSpan(0, 0, 15);
		private readonly TimeSpan periodTime = new TimeSpan(0, 0, 60);
		private readonly SiliconLabsSI7005 sensor = new SiliconLabsSI7005();
		private readonly Led _led = new Led();
		private readonly byte[] fieldGatewayAddress = Encoding.UTF8.GetBytes("LoRaIoT1");
		private readonly byte[] deviceAddress = Encoding.UTF8.GetBytes("Nexus915");

		public NexusClient()
		{
			rfm9XDevice = new Rfm9XDevice(SPI.SPI_module.SPI3, (Cpu.Pin)28, (Cpu.Pin)15, (Cpu.Pin)26);
			_led.Set(0, 0, 0);
		}

		public void Run()
		{

			rfm9XDevice.Initialise(frequency: 915000000, paBoost: true, rxPayloadCrcOn: true);
			rfm9XDevice.Receive(deviceAddress);

			rfm9XDevice.OnDataReceived += rfm9XDevice_OnDataReceived;
			rfm9XDevice.OnTransmit += rfm9XDevice_OnTransmit;

			Timer humidityAndtemperatureUpdates = new Timer(HumidityAndTemperatureTimerProc, null, dueTime, periodTime);

			Thread.Sleep(Timeout.Infinite);
		}


		private void HumidityAndTemperatureTimerProc(object state)
		{
			_led.Set(0, 128, 0);

			double humidity = sensor.Humidity();
			double temperature = sensor.Temperature();

			Debug.Print(DateTime.UtcNow.ToString("hh:mm:ss") + " H:" + humidity.ToString("F1") + " T:" + temperature.ToString("F1"));

			rfm9XDevice.Send(fieldGatewayAddress, Encoding.UTF8.GetBytes("t " + temperature.ToString("F1") + ",H " + humidity.ToString("F0")));
		}

		void rfm9XDevice_OnTransmit()
		{
			_led.Set(0, 0, 0);

			Debug.Print("Transmit-Done");
		}

		void rfm9XDevice_OnDataReceived(byte[] address, float packetSnr, int packetRssi, int rssi, byte[] data)
		{
			try
			{
				string messageText = new string(UTF8Encoding.UTF8.GetChars(data));
				string addressText = new string(UTF8Encoding.UTF8.GetChars(address));

				Debug.Print(DateTime.UtcNow.ToString("HH:MM:ss") + "-Rfm9X PacketSnr " + packetSnr.ToString("F1") + " Packet RSSI " + packetRssi + "dBm RSSI " + rssi + "dBm = " + data.Length + " byte message " + @"""" + messageText + @"""");
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message);
			}
		}
	}
}
