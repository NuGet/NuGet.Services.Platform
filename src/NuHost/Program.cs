// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Services.Hosting;
using NuGet.Services.ServiceModel;
using PowerArgs;

namespace NuHost
{
    public class Program
    {
        static void Main(string[] args)
        {
            var semVerAttr = typeof(Program).GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var commitMetaAttr = typeof(Program).GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => String.Equals(a.Key, "CommitId", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("NuHost v{0} (Commit {1})",
                semVerAttr == null ? typeof(Program).Assembly.GetName().Version.ToString() : semVerAttr.InformationalVersion,
                commitMetaAttr == null ? "UNKNOWN" : commitMetaAttr.Value);

            // Set up the app domain resolution
            ResolveAssembliesFromResources();

            new Program().Run(args);
        }

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

        private static void ResolveAssembliesFromResources()
        {
            var cache = new Dictionary<string, Assembly>();
            AppDomain.CurrentDomain.AssemblyResolve += (a, b) =>
            {
                Assembly assembly;
                if (cache.TryGetValue(b.Name, out assembly))
                {
                    return assembly;
                }

                // Look for a resource
                var asmName = new AssemblyName(b.Name);
                var strm = typeof(Program).Assembly.GetManifestResourceStream(
                    "EmbeddedAssemblies." + asmName.Name + ".dll");
                if (strm != null)
                {
                    // Load it and use it
                    using (strm)
                    using (var ms = new MemoryStream())
                    {
                        strm.CopyTo(ms);
                        assembly = Assembly.Load(ms.ToArray());
                    }
                }
                cache[b.Name] = assembly;
                if (assembly != null)
                {
                    cache[assembly.FullName] = assembly;
                }
                return assembly;
            };
        }

        public void Run(string[] args) 
        {
            // Parse args
            if (args.Length > 0 && String.Equals("dbg", args[0], StringComparison.OrdinalIgnoreCase))
            {
                args = args.Skip(1).ToArray();
                Debugger.Launch();
            }

            var parsed = Args.Parse<Arguments>(args);
            if (parsed.Help)
            {
                ArgUsage.GenerateUsageFromTemplate<Arguments>().Write();
                return;
            }

            // Set defaults
            string inferredConfig = null;
            parsed.BaseDirectory = Path.GetFullPath(String.IsNullOrEmpty(parsed.BaseDirectory) ? 
                InferBaseDirectory(out inferredConfig) :
                parsed.BaseDirectory);
            Console.WriteLine("Using Base Directory: " + parsed.BaseDirectory);

            // Find services platform
            var platformAssemblyFile = Path.Combine(parsed.BaseDirectory, "NuGet.Services.Platform.dll");
            if (!File.Exists(platformAssemblyFile))
            {
                Console.Error.WriteLine("Unable to locate NuGet.Services.Platform.dll in base directory!");
                return;
            }

            // Check for nuhost file
            var configFile = Path.Combine(parsed.BaseDirectory, "nuhost.json");
            var config = File.Exists(configFile) ?
                NuHostConfig.Load(configFile) :
                NuHostConfig.Default;

            config.ClrConfigFile = String.IsNullOrEmpty(config.ClrConfigFile) ?
                inferredConfig :
                config.ClrConfigFile;

            if (!String.IsNullOrEmpty(config.ClrConfigFile))
            {
                if (!Path.IsPathRooted(config.ClrConfigFile))
                {
                    config.ClrConfigFile = Path.Combine(parsed.BaseDirectory, config.ClrConfigFile);
                }
                Console.WriteLine("Using CLR config file: " + config.ClrConfigFile);
            }

            ResolveAssembliesFromDirectory(parsed.BaseDirectory);
            
            LoadAndStartApp(parsed, config);
        }

        private string InferBaseDirectory(out string inferredConfigFile)
        {
            inferredConfigFile = null;
            string baseDir;

            // Are we in a NuGet Repo?
            var repoFile = Path.Combine(Environment.CurrentDirectory, "Repository.props");
            if (File.Exists(repoFile))
            {
                baseDir = InferFromRepositoryRoot(out inferredConfigFile);
                if (!String.IsNullOrEmpty(baseDir))
                {
                    return baseDir;
                }
            }

            // Nope, are we in a project directory?
            var match = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").FirstOrDefault();
            if (match != null)
            {
                baseDir = InferFromProject(match, out inferredConfigFile);
                if (!String.IsNullOrEmpty(baseDir))
                {
                    return baseDir;
                }
            }

            // Nope, well just use the current directory then
            return Environment.CurrentDirectory;
        }

        private string InferFromProject(string projectFile, out string inferredConfigFile)
        {
            inferredConfigFile = null;

            // Get the project name
            string name = Path.GetFileNameWithoutExtension(projectFile);

            // Check for the code
            string outputDir = Path.Combine(Path.GetDirectoryName(projectFile), @"bin\Debug");
            string dll = Path.Combine(outputDir, name + ".dll");
            string config = Path.Combine(outputDir, name + ".dll.config");
            if (File.Exists(dll))
            {
                if (File.Exists(config))
                {
                    inferredConfigFile = config;
                }
                return outputDir;
            }
            return null;
        }

        private string InferFromRepositoryRoot(out string inferredConfigFile)
        {
            inferredConfigFile = null;

            // Check the current directory name
            var repoName = Path.GetFileName(Environment.CurrentDirectory);

            // Check for a project
            string csproj = Path.Combine(Environment.CurrentDirectory, @"src\" + repoName + @"\" + repoName + ".csproj");
            if (File.Exists(csproj))
            {
                return InferFromProject(csproj, out inferredConfigFile);
            }
            return null;
        }

        private void LoadAndStartApp(Arguments parsed, NuHostConfig config)
        {
            // Create start options
            NuGetDomainStartOptions options = new NuGetDomainStartOptions()
            {
                HostApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                ApplicationBase = parsed.BaseDirectory
            };
            if (!String.IsNullOrEmpty(parsed.Configuration))
            {
                options.Configuration = JsonConvert.DeserializeObject<Dictionary<string, string>>(parsed.Configuration);
            }

            options.AppDescription = new ServiceHostDescription(
                ServiceHostInstanceName.Parse("nuget-local-0-nuhost_IN" + Process.GetCurrentProcess().Id.ToString()),
                Environment.MachineName);
            if (parsed.Services != null)
            {
                options.Services = parsed.Services.ToList();
            }
            var urls = parsed.Urls ?? NuGetApp.GetUrls(parsed.HttpPort, parsed.HttpsPort, parsed.HttpPath, localOnly: true);
            foreach (var url in urls)
            {
                options.Urls.Add(url);
            }

            // Create the AppDomain
            var setup = new AppDomainSetup()
            {
                ApplicationBase = options.ApplicationBase,
                ConfigurationFile = config.ClrConfigFile
            };
            var domain = AppDomain.CreateDomain("NuGetServices", null, setup);
            dynamic agent = domain.CreateInstanceAndUnwrap(
                "NuGet.Services.Platform",
                "NuGet.Services.Hosting.ConsoleApplicationHost");
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
