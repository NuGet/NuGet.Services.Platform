// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace NuGet.Services.Hosting
{
    public class ConsoleApplicationHost : MarshalByRefObject, IApplicationHost
    {
        public void Run(NuGetDomainStartOptions options)
        {
            RunAsync(options).Wait();
        }

        public async Task RunAsync(NuGetDomainStartOptions options)
        {
            try
            {
                ResolveAssembliesFromDirectory(options.HostApplicationBase);

                // Load all assemblies in my app domain
                foreach (var dllFile in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
                {
                    try
                    {
                        Assembly.Load(Path.GetFileNameWithoutExtension(dllFile));
                    }
                    catch (Exception)
                    {
                        // Ignore load failures here
                    }
                }

                var app = NuGetApp.Create(options);
                Console.WriteLine(Strings.ConsoleApplicationHost_Running);
                app.EventStream.Subscribe(ev =>
                {
                    Console.WriteLine("[{0}]({1:000}) {2}", ev.Schema.ProviderName, ev.EventId, ev.FormattedMessage);
                });
                app.Initialize();
                if (!await app.Start())
                {
                    Console.WriteLine(Strings.ConsoleApplicationHost_FailedToStart);
                }
                else
                {
                    Console.CancelKeyPress += (sender, args) =>
                    {
                        args.Cancel = true;
                        app.Shutdown();
                    };
                    await app.Run();
                }
            }
            catch (AggregateException aex)
            {
                Console.WriteLine(aex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ResolveAssembliesFromDirectory(string directory)
        {
            // Resolve relative paths
            directory = Path.GetFullPath(directory);

            var cache = new Dictionary<string, Assembly>();
            AppDomain.CurrentDomain.AssemblyResolve +=
                (a, b) =>
                {
                    Assembly assembly;
                    if (cache.TryGetValue(b.Name, out assembly))
                    {
                        return assembly;
                    }

                    string shortName = new AssemblyName(b.Name).Name;
                    string path = Path.Combine(directory, shortName + ".dll");

                    // Load .dll?
                    if (File.Exists(path))
                    {
                        assembly = Assembly.LoadFile(path);
                    }
                    // Try .exe?
                    else if (File.Exists(path = Path.Combine(directory, shortName + ".exe")))
                    {
                        assembly = Assembly.LoadFile(path);
                    }

                    cache[b.Name] = assembly;
                    if (assembly != null)
                    {
                        cache[assembly.FullName] = assembly;
                    }
                    return assembly;
                };
        }

        public static void LaunchInNewAppDomain(NuGetDomainStartOptions options)
        {
            // Find services platform
            var platformAssemblyFile = Path.Combine(options.ApplicationBase, typeof(ConsoleApplicationHost).Assembly.GetName().Name + ".dll");
            if (!File.Exists(platformAssemblyFile))
            {
                throw new InvalidOperationException("Unable to locate NuGet.Services.Platform.dll in base directory!");
            }

            // Create the AppDomain
            var setup = new AppDomainSetup()
            {
                ApplicationBase = options.ApplicationBase
            };
            var domain = AppDomain.CreateDomain("NuGetServices", null, setup);
            dynamic agent = domain.CreateInstanceFromAndUnwrap(
                platformAssemblyFile,
                typeof(ConsoleApplicationHost).FullName);
            try
            {
                agent.Run(options);
            }
            catch (AggregateException aex)
            {
                Console.WriteLine(aex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
