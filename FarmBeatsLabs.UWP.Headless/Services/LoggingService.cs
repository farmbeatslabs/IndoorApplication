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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace FarmBeatsLabs.UWP.Headless.Services
{
    public static class LoggingService
    {
        private static string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
        private static Guid guid = new Guid("4bd2826e-54a1-4ba9-bf63-92b73ea1ac4a");

        private static readonly LoggingChannel loggingChannel = new LoggingChannel(assemblyName, null, guid);

        public static void Log(string message) => Log(message, LoggingLevel.Information);

        public static void Log(string message, LoggingLevel level)
        {
            loggingChannel.LogMessage(message, level);
#if DEBUG
            Debug.WriteLine(message);
#endif
        }

        public static void Error(string message, Exception ex)
        {
            var msg = $"{message}\r\nException: {ex}";
            Log(msg, LoggingLevel.Error);
        }

        public static void LogEvent(string eventName, LoggingFields fields) => LogEvent(eventName, fields, LoggingLevel.Information);
        public static void LogEvent(string eventName, LoggingFields fields, LoggingLevel level) => loggingChannel.LogEvent(eventName, fields, level);
    }
}
