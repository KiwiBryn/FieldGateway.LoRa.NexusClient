using System;
using Microsoft.SPOT.Hardware;

namespace IngenuityMicro.Nexus
{
    public class Led
    {
        #region private fields

        private readonly SPI _spi;
        private readonly byte[] _ledTable;    // The conversion table between byte value and SPI coding
        private readonly byte[] _ledBuffer;   // The buffer allocated to store and send the led states (12x led number)

        #endregion

        /// <summary>
        /// LED driver for Nexus
        /// </summary>

        public Led()
        {
            var spiConfig = new SPI.Configuration(Cpu.Pin.GPIO_NONE, false, 500, 500, false, false, 3200, SPI.SPI_module.SPI2);
            _spi = new SPI(spiConfig);
            _ledBuffer = new byte[12];
            // Compute fast byte to SPI lookup table. By default the linearisation of human perceived luminosity is on (Weber–Fechner law)
            _ledTable = new byte[1024];
            BuildTable();
        }
        

        /// <summary>
        /// (re)build the lookup table to switch between human perceived luminosity to real PWM values.
        /// </summary>
        /// <param name="linearization">if >0, will linearize to human perception using this param as a factor (2.25 by default)</param>
        public void BuildTable(double linearization = 2.25)
        {
            // Compute fast byte to SPI lookup table
            byte tmp;

            for (int i = 0, ptr = 0; i <= 255; i++)
            {
                // Depending on the value of linear, we use a formula to linearize or direct value
                tmp = (byte)(linearization > 0 ? System.Math.Round(System.Math.Pow(i, linearization) / System.Math.Pow(255, linearization) * 255) : i);
                for (int j = 6; j >= 0; j -= 2)
                {
                    switch (tmp >> j & 3)
                    {
                        case 0: _ledTable[ptr++] = 0x88; break;
                        case 1: _ledTable[ptr++] = 0x8C; break;
                        case 2: _ledTable[ptr++] = 0xC8; break;
                        case 3: _ledTable[ptr++] = 0xCC; break;
                    }
                }
            }
        }
        /// <summary>
        /// Sets led color.
        /// </summary>
        /// <param name="r">Red value, from 0 = dark to 255 = brigther</param>
        /// <param name="g">Green value, from 0 = dark to 255 = brigther</param>
        /// <param name="b">Blue value, from 0 = dark to 255 = brigther</param>
        public void Set(byte r, byte g, byte b) 
        {
            Array.Copy(_ledTable, g << 2, _ledBuffer, 0, 4);
            Array.Copy(_ledTable, r << 2, _ledBuffer, 0 + 4, 4);
            Array.Copy(_ledTable, b << 2, _ledBuffer, 0 + 8, 4);
            _spi.Write(_ledBuffer);
        }

        /// <summary>
        /// Convert HSV to RGB
        /// h is from 0-360
        /// s,v values are 0-1
        /// r,g,b values are 0-255
        /// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
        /// </summary>
        public void HsvToRgb(double h, double S, double V, out byte r, out byte g, out byte b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)System.Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        private static byte Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return (byte)i;
        }
    }
}
