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
using Newtonsoft.Json;

namespace FarmBeatsLabs.UWP.Headless.Models
{
    public sealed class TelemetryDataPoint
    {
        [JsonProperty("AirTemperatureC")]
        public double Celsius { get; set; }
        [JsonProperty("AirTemperatureF")]
        public double Fahrenheit { get; set; }
        [JsonProperty("AirHumidity")]
        public double Humidity { get; set; }
        [JsonProperty("AirPressure")]
        public double Pressure { get; set; }
        public double Light { get; set; }
        public double SoilMoisture1 { get; set; }
        public double SoilMoisture2 { get; set; }
    }
}
