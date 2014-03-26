using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Hosting
{
    public class NuGetApp : IDisposable
    {
        public LocalServiceHost ServiceHost { get; private set; }
        public IObservable<EventEntry> EventStream { get { return ServiceHost.EventStream; } }

        public NuGetApp(LocalServiceHost host)
        {
            ServiceHost = host;
        }

        public static Task<NuGetApp> Start(string name, string url, params ServiceDefinition[] services)
        {
            return Start(BuildOptions(name, url, services));
        }

        public static Task<NuGetApp> Start(string name, int httpPort, params ServiceDefinition[] services)
        {
            return Start(BuildOptions(name, httpPort, null, services));
        }

        public static Task<NuGetApp> Start(string name, int httpPort, int httpsPort, params ServiceDefinition[] services)
        {
            return Start(BuildOptions(name, httpPort, httpsPort, services));
        }

        public static async Task<NuGetApp> Start(NuGetStartOptions options)
        {
            // Create a host
            var host = new LocalServiceHost(options);

            // Wrap it up in a NuGetApp and start it
            var app = new NuGetApp(host);
            if (await app.Start())
            {
                return app;
            }
            return null;
        }

        public Task<bool> Start()
        {
            // Start the host
            return ServiceHost.Start();
        }

        public Task Run()
        {
            return ServiceHost.Run();
        }

        public void Shutdown()
        {
            ServiceHost.Shutdown();
        }

        private static NuGetStartOptions BuildOptions(string name, string url, ServiceDefinition[] services)
        {
            return BuildOptions(name, new[] { url }, services);
        }

        private static NuGetStartOptions BuildOptions(string name, int? httpPort, int? httpsPort, ServiceDefinition[] services)
        {
            return BuildOptions(name, GetUrls(httpPort, httpsPort, String.Empty), services);
        }

        private static NuGetStartOptions BuildOptions(string name, IEnumerable<string> urls, IEnumerable<ServiceDefinition> services)
        {
            var options = new NuGetStartOptions();
            
            foreach (var url in urls)
            {
                options.Urls.Add(url);
            }
            
            foreach (var service in services)
            {
                options.Services.Add(service);
            }

            options.AppDescription = new ServiceHostDescription(
                ServiceHostInstanceName.Parse("nuget-local-0-" + name + "_IN0"),
                Environment.MachineName);

            return options;
        }

        internal static IEnumerable<string> GetUrls(int? httpPort, int? httpsPort, string basePath)
        {
            if (httpPort != null)
            {
                yield return "http://+:" + httpPort.Value.ToString() + "/" + basePath;
                yield return "http://localhost:" + httpPort.Value.ToString() + "/" + basePath;
            }

            if (httpsPort != null)
            {
                yield return "http://+:" + httpsPort.Value.ToString() + "/" + basePath;
                yield return "http://localhost:" + httpsPort.Value.ToString() + "/" + basePath;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
