//---------------------------------------------------------------------------------
// Copyright (c) 2016, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//---------------------------------------------------------------------------------
namespace devMobile.NetMF.Sensor
{
   using System;
   using System.Threading;
   using Microsoft.SPOT;
   using Microsoft.SPOT.Hardware;


   public class SiliconLabsSI7005
   {
      private const byte DeviceId = 0x40;
      private const int ClockRateKHz = 400;
      private const int TransactionTimeoutMilliseconds = 1000;

      private const int RegisterIdConfiguration = 0x03;
      private const int RegisterIdStatus = 0x00;
      private const byte RegisterIdDeviceId = 0x11;
		private const byte RegisterDeviceId = 0x50;
		private const byte StatusMask = 0x01;
      private const byte CommandMeasureTemperature = 0x11;  
      private const byte CommandMeasureHumidity = 0x01;
      private const byte ConversionDataRegister = 0x01;


      public SiliconLabsSI7005()
      {
         using (I2CDevice device = new I2CDevice(new I2CDevice.Configuration(DeviceId, ClockRateKHz)))
         {
            byte[] writeBuffer = { RegisterIdDeviceId };
            byte[] readBuffer = new byte[1];

            // check that sensor connected
            I2CDevice.I2CTransaction[] action = new I2CDevice.I2CTransaction[] 
            { 
               I2CDevice.CreateWriteTransaction(writeBuffer),
               I2CDevice.CreateReadTransaction(readBuffer)
            };

            if (device.Execute(action, TransactionTimeoutMilliseconds) == 0)
				{
					// The first read always fails
				}
				if (device.Execute(action, TransactionTimeoutMilliseconds) == 0)
				{
					throw new Exception("Unable to read DeviceId");
				}
				if (readBuffer[0] != RegisterDeviceId)
				{
					throw new Exception("DeviceId invalid");
				}
			}
		}



      public double Temperature()
      {
         using (I2CDevice device = new I2CDevice(new I2CDevice.Configuration(DeviceId, ClockRateKHz)))
         {
            //Debug.Print("Temperature Measurement start");

            byte[] CmdBuffer = { RegisterIdConfiguration, CommandMeasureTemperature };

            I2CDevice.I2CTransaction[] CmdAction = new I2CDevice.I2CTransaction[] 
            { 
               I2CDevice.CreateWriteTransaction(CmdBuffer),
            };

            if (device.Execute(CmdAction, TransactionTimeoutMilliseconds) == 0)
            {
               throw new Exception("Unable to send Measure temperature command");
            }


            //Debug.Print("Measurement wait");
            bool conversionInProgress = true;

            // Wait for measurement
            do
            {
               byte[] WaitWriteBuffer = { RegisterIdStatus };
               byte[] WaitReadBuffer = new byte[1];

               I2CDevice.I2CTransaction[] waitAction = new I2CDevice.I2CTransaction[] 
               { 
                  I2CDevice.CreateWriteTransaction(WaitWriteBuffer),
                  I2CDevice.CreateReadTransaction(WaitReadBuffer)
               };

               if (device.Execute(waitAction, TransactionTimeoutMilliseconds) == 0)
               {
                  throw new Exception("Unable to read status register");
               }

               if ((WaitReadBuffer[RegisterIdStatus] & StatusMask) != StatusMask)
               {
                  conversionInProgress = false;
               }
            } while (conversionInProgress);


            // Debug.Print("Measurement read");
            // Read temperature value
            byte[] valueWriteBuffer = { ConversionDataRegister };
            byte[] valueReadBuffer = new byte[2];

            I2CDevice.I2CTransaction[] valueAction = new I2CDevice.I2CTransaction[] 
            { 
               I2CDevice.CreateWriteTransaction(valueWriteBuffer),
               I2CDevice.CreateReadTransaction(valueReadBuffer)
            };

            if (device.Execute(valueAction, TransactionTimeoutMilliseconds) == 0)
            {
               throw new Exception("Unable to read data register");
            }

            // Convert bye to centigrade
            int temp = valueReadBuffer[0];

            temp = temp << 8;
            temp = temp + valueReadBuffer[1];
            temp = temp >> 2;
            /* 
              Formula: Temperature(C) = (Value/32) - 50	  
            */
            double temperature = (temp / 32.0) - 50.0;

            //Debug.Print(" Temp " + temperature.ToString("F1"));

            return temperature;
         }
      }



      public double Humidity()
      {
         using (I2CDevice device = new I2CDevice(new I2CDevice.Configuration(DeviceId, ClockRateKHz)))
         {
            //Debug.Print("Humidity Measurement start");

            byte[] CmdBuffer = { RegisterIdConfiguration, CommandMeasureHumidity };

            I2CDevice.I2CTransaction[] CmdAction = new I2CDevice.I2CTransaction[] 
            { 
               I2CDevice.CreateWriteTransaction(CmdBuffer),
            };

            if (device.Execute(CmdAction, TransactionTimeoutMilliseconds) == 0)
            {
               throw new Exception("Unable to send Measure temperature command");
            }

            //Debug.Print("Measurement wait");
            bool humidityConversionInProgress = true;

            // Wait for measurement
            do
            {
               byte[] WaitWriteBuffer = { RegisterIdStatus };
               byte[] ValueReadBuffer = new byte[1];

               I2CDevice.I2CTransaction[] waitAction = new I2CDevice.I2CTransaction[] 
               { 
                  I2CDevice.CreateWriteTransaction(WaitWriteBuffer),
                  I2CDevice.CreateReadTransaction(ValueReadBuffer)
               };

               if (device.Execute(waitAction, TransactionTimeoutMilliseconds) == 0)
               {
                  throw new Exception("Unable to read status register");
               }

               if ((ValueReadBuffer[RegisterIdStatus] & StatusMask) != StatusMask)
               {
                  humidityConversionInProgress = false;
               }
            } while (humidityConversionInProgress);


            //Debug.Print("Measurement read");
            byte[] valueWriteBuffer = { ConversionDataRegister };
            byte[] valueRreadBuffer = new byte[2];

            I2CDevice.I2CTransaction[] valueAction = new I2CDevice.I2CTransaction[] 
            { 
               I2CDevice.CreateWriteTransaction(valueWriteBuffer),
               I2CDevice.CreateReadTransaction(valueRreadBuffer)
            };

            if (device.Execute(valueAction, TransactionTimeoutMilliseconds) == 0)
            {
               throw new Exception("Unable to read data register");
            }

            int hum = valueRreadBuffer[0];

            hum = hum << 8;
            hum = hum + valueRreadBuffer[1];
            hum = hum >> 4;
            /* 
            Formula:
            Humidity(%) = (Value/16) - 24	  
            */
            double humidity = (hum / 16.0) - 24.0;

            //Debug.Print(" Humidity " + humidity.ToString("F1"));
            return humidity;
         }
      }
   }
}
