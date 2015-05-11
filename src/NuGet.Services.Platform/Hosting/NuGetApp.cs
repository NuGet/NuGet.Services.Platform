// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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

        public static NuGetApp Create(string name, string url, params string[] services)
        {
            return Create(BuildOptions(name, url, services));
        }

        public static NuGetApp Create(string name, int httpPort, params string[] services)
        {
            return Create(BuildOptions(name, httpPort, null, services));
        }

        public static NuGetApp Create(string name, int httpPort, int httpsPort, params string[] services)
        {
            return Create(BuildOptions(name, httpPort, httpsPort, services));
        }

        public static NuGetApp Create(NuGetStartOptions options)
        {
            // Create a host
            var host = new LocalServiceHost(options);

            // Wrap it up in a NuGetApp and initialize it
            return new NuGetApp(host);
        }

        public void Initialize()
        {
            ServiceHost.Initialize();
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

        private static NuGetStartOptions BuildOptions(string name, string url, IEnumerable<string> services)
        {
            return BuildOptions(name, new[] { url }, services);
        }

        private static NuGetStartOptions BuildOptions(string name, int? httpPort, int? httpsPort, IEnumerable<string> services)
        {
            return BuildOptions(name, GetUrls(httpPort, httpsPort, String.Empty, localOnly: false), services);
        }

        private static NuGetStartOptions BuildOptions(string name, IEnumerable<string> urls, IEnumerable<string> services)
        {
            var options = new NuGetStartOptions();
            
            foreach (var url in urls)
            {
                options.Urls.Add(url);
            }

            options.Services = services;

            options.AppDescription = new ServiceHostDescription(
                ServiceHostInstanceName.Parse("nuget-local-0-" + name + "_IN0"),
                Environment.MachineName);

            return options;
        }

        public static IEnumerable<string> GetUrls(int? httpPort, int? httpsPort, string basePath, bool localOnly)
        {
            if (httpPort != null)
            {
                if (!localOnly)
                {
                    yield return "http://+:" + httpPort.Value.ToString() + "/" + basePath;
                }
                yield return "http://localhost:" + httpPort.Value.ToString() + "/" + basePath;
            }

            if (httpsPort != null)
            {
                if (!localOnly)
                {
                    yield return "https://+:" + httpsPort.Value.ToString() + "/" + basePath;
                }
                yield return "https://localhost:" + httpsPort.Value.ToString() + "/" + basePath;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
