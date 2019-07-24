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
using System.ComponentModel;
using System.Net;
using FarmBeatsLabs.UWP.Headless.Models;
using Newtonsoft.Json;

namespace FarmBeatsLabs.UWP.Headless.Services
{
    public static class AutodiscoverService
    {
        private static WebClient webclient = new WebClient();
        private static string autodiscoverUrl = @"https://farmbeatslabs-servicediscovery.azurewebsites.net/api/autodiscover/{0}?code=3jn2T4QTFV/9mgw1A78ISpH4A9wvC0JKavbgIYaEK05XOJFKNdc7AQ==";

        public static AutodiscoverResult GetDeviceSettings(string deviceId)
        {
            //look up device in discovery service
            var url = string.Format(autodiscoverUrl, deviceId);
            var json = webclient.DownloadString(url);
            var resp = JsonConvert.DeserializeObject<AutodiscoverResult>(json);

            if (string.IsNullOrEmpty(resp.ConnectionString?.Trim()) == true) throw new Exception("Failed to find connection string.");
            else return resp;
        }
    }
}
