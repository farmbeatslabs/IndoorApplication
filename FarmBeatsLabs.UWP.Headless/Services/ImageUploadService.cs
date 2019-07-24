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
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace FarmBeatsLabs.UWP.Headless.Services
{
    public static class ImageUploadService
    {
        private const string imageUploadUrl = @"https://farmbeatslabs-servicediscovery.azurewebsites.net/api/image/{0}?code=PoaSfZUzLaztnd/fGBhvp5fo7KU6MO6JAgIWfMqduRurYsLSMiL72Q=="; 

        public static void Upload(string deviceId, IInputStream captureStream)
        {
            Uri uri = new Uri(string.Format(imageUploadUrl, deviceId));

            using (HttpClient httpClient = new HttpClient())
            {
                using (HttpStreamContent streamContent = new HttpStreamContent(captureStream))
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri))
                    {
                        streamContent.Headers.ContentType = HttpMediaTypeHeaderValue.Parse("image/jpeg");
                        request.Content = streamContent;

                        Debug.WriteLine("SendRequestAsync start");
                        HttpResponseMessage response = httpClient.SendRequestAsync(request).AsTask().Result;
                        Debug.WriteLine("SendRequestAsync finish");

                        response.EnsureSuccessStatusCode();
                    }
                }
            }
        }
    }
}
