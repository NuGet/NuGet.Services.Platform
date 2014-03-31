using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Hosting;
using NuGet.Services.ServiceModel;
using PowerArgs;

namespace NuHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT
            // DO NOT reference ANY NuGet.* assemblies in this method
            // All code in this method must be able to run using ONLY NuHost.exe, the BCL and the Copy-Local
            // references (PowerArgs, etc.).
            // The NuGet.* assemblies are intentionally NOT marked as Copy-Local so that they can be resolved
            // local to the 

            if (args.Length > 0 && String.Equals("dbg", args[0], StringComparison.OrdinalIgnoreCase))
            {
                args = args.Skip(1).ToArray();
                Debugger.Launch();
            }

            var parsed = Args.Parse<Arguments>(args);

            // Set defaults
            parsed.BaseDirectory = parsed.BaseDirectory ?? Environment.CurrentDirectory;

            // From here on, resolve assemblies based on the Base Directory we were provided, instead of the
            // base directory of the EXE itself.
            ResolveAssembliesFromDirectory(parsed.BaseDirectory);

            // Now, in our new Assembly Resolution context, Run ALL THE THINGS!
            RunServices(parsed).Wait();
        }

        static async Task RunServices(Arguments parsed)
        {
            // Create start options
            NuGetStartOptions options = new NuGetStartOptions();
            options.AppDescription = new ServiceHostDescription(
                ServiceHostInstanceName.Parse("nuget-local-0-nuhost_IN" + Process.GetCurrentProcess().Id.ToString()),
                Environment.MachineName);
            if (parsed.Services != null)
            {
                options.Services = parsed.Services.ToList();
            }
            var urls = parsed.Urls ?? NuGetApp.GetUrls(parsed.HttpPort, parsed.HttpsPort, parsed.BasePath, localOnly: true);
            foreach (var url in urls)
            {
                options.Urls.Add(url);
            }

            // Load assemblies in the current directory
            var asmNames = Directory
                .GetFiles(parsed.BaseDirectory)
                .Select(s => Path.GetFileNameWithoutExtension(s))
                .Distinct();
            foreach (var asmName in asmNames)
            {
                try
                {
                    Assembly.Load(asmName);
                }
                catch (Exception)
                {
                    // We're just pre-loading the assemblies, no worries if there's a failure
                }
            }

            // Run!
            var app = NuGetApp.Create(options);
            
            Console.WriteLine("Starting the service. Press Ctrl-C to shutdown");
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                app.Shutdown();
            };
            
            app.EventStream.Subscribe(new ConsoleLoggingObserver());
            
            await app.Start();
            
            await app.Run();
        }

        // Borrowed from Katana (http://katanaproject.codeplex.com/SourceControl/latest#src/OwinHost/Program.cs)
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Justification = "By design")]
        private static void ResolveAssembliesFromDirectory(string directory)
        {
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
                    if (File.Exists(path))
                    {
                        assembly = Assembly.LoadFrom(path);
                    }
                    cache[b.Name] = assembly;
                    if (assembly != null)
                    {
                        cache[assembly.FullName] = assembly;
                    }
                    return assembly;
                };
        }
    }
}
