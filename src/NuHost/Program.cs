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
    class Program : MarshalByRefObject
    {
        private TaskCompletionSource<bool> _startTcs = new TaskCompletionSource<bool>();
        private TaskCompletionSource<object> _runTcs = new TaskCompletionSource<object>();

        static void Main(string[] args)
        {
            new Program().Run(args).Wait();
        }

        public async Task Run(string[] args) 
        {
            if (args.Length > 0 && String.Equals("dbg", args[0], StringComparison.OrdinalIgnoreCase))
            {
                args = args.Skip(1).ToArray();
                Debugger.Launch();
            }

            var parsed = Args.Parse<Arguments>(args);

            // Set defaults
            parsed.BaseDirectory = parsed.BaseDirectory ?? Environment.CurrentDirectory;

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

            // Find services platform
            var platformAssemblyFile = Path.Combine(parsed.BaseDirectory, typeof(NuGetDomainAgent).Assembly.GetName().Name + ".dll");
            if (!File.Exists(platformAssemblyFile))
            {
                throw new InvalidOperationException("Unable to locate NuGet.Services.Platform.dll in base directory!");
            }

            // Create the AppDomain
            var setup = new AppDomainSetup()
            {
                ApplicationBase = parsed.BaseDirectory
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
