using Hzdtf.Consul.Extensions.Common;
using Hzdtf.Utility.RemoteService.Builder;
using Hzdtf.Utility.RemoteService.Options;
using System;
using System.Net.Http;
using System.Threading;

namespace Hzdtf.Consul.ClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("start test...");

            var serviceProvider = new ConsulServiceProviderAgg();
            var serviceOptions = new UnityServicesOptionsCache();
            var unityServicesBuilder = new UnityServicesBuilder(serviceProvider, serviceOptions);

            using (var httpClient = new HttpClient())
            {
                for (var i = 0; i < 20; i++)
                {
                    var url = unityServicesBuilder.BuilderAsync("ServiceExampleA", "/Health", "M1").Result;
                    var content = httpClient.GetStringAsync(url).Result;

                    Console.WriteLine($"第{i}次请求[{url}]:{content}");

                    Thread.Sleep(1000);
                }
            }

            Console.ReadLine();
        }
    }
}
