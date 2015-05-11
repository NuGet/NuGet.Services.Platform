// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.ResolveAnything;
using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using NuGet.Services.Configuration;
using NuGet.Services.Http;
using NuGet.Services.Models;
using NuGet.Services.Http.Middleware;
using NuGet.Services.Http.Authentication;
using NuGet.Services.Http.Models;
using Microsoft.Owin.Security;
using System.Diagnostics.Tracing;
using NuGet.Services.Work.Azure;

namespace NuGet.Services.ServiceModel
{
    public abstract class ServiceHost
    {
        private CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        private IContainer _container;
        private AssemblyInformation _runtimeInformation = typeof(ServiceHost).GetAssemblyInfo();
        private IDisposable _httpServerLifetime;
        private TaskCompletionSource<object> _shutdownTcs = new TaskCompletionSource<object>();
        
        private volatile int _nextId = 0;

        public abstract ServiceHostDescription Description { get; }

        public CancellationToken ShutdownToken { get { return _shutdownTokenSource.Token; } }

        public ConfigurationHub Config { get; private set; }
        public IReadOnlyDictionary<string, ServiceDefinition> Services { get; private set; }
        public IReadOnlyList<NuGetService> Instances { get; private set; }
        public IReadOnlyList<NuGetHttpService> HttpServiceInstances { get; private set; }

        private IReadOnlyDictionary<Type, NuGetService> InstancesByType { get; set; }
        private IReadOnlyDictionary<string, NuGetService> InstancesByName { get; set; }

        public AssemblyInformation RuntimeInformation { get { return _runtimeInformation; } }

        protected ServiceHost()
        {
            _shutdownTokenSource.Token.Register(() => _shutdownTcs.TrySetResult(null));
        }

        /// <summary>
        /// Starts all services in the host and blocks until they have completed starting.
        /// </summary>
        public virtual bool StartAndWait()
        {
            return Start().Result;
        }

        /// <summary>
        /// Starts all services, returning a task that will complete when they have completed starting
        /// </summary>
        public virtual async Task<bool> Start()
        {
            var instances = await Task.WhenAll(Services.Values.Select(StartService));
            HttpServiceInstances = instances.OfType<NuGetHttpService>().ToList().AsReadOnly();

            ServicePlatformEventSource.Log.StartingHttpServices(Description.InstanceName);
            try
            {
                _httpServerLifetime = StartHttp(HttpServiceInstances);
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.ErrorStartingHttpServices(Description.InstanceName, ex);
                throw;
            }
            ServicePlatformEventSource.Log.StartedHttpServices(Description.InstanceName);

            Instances = instances.Where(s => s != null).ToList().AsReadOnly();
            InstancesByType = new ReadOnlyDictionary<Type, NuGetService>(Instances.ToDictionary(s => s.GetType()));
            InstancesByName = new ReadOnlyDictionary<string, NuGetService>(Instances.ToDictionary(s => s.ServiceName.Name, StringComparer.OrdinalIgnoreCase));

            return instances.All(s => s != null);
        }

        /// <summary>
        /// Runs all services, returning a task that will complete when they stop
        /// </summary>
        public virtual async Task Run()
        {
            await Task.WhenAll(Instances.Select(RunService));
            foreach (var instance in Instances)
            {
                instance.Dispose();
            }
            ServicePlatformEventSource.Log.CleanShutdown(Description.InstanceName);
        }

        /// <summary>
        /// Requests that all services shut down. Calling this will cause the task returned by Run to complete (eventually)
        /// </summary>
        public virtual void Shutdown()
        {
            ServicePlatformEventSource.Log.HostShutdownRequested(Description.InstanceName.ToString());
            _shutdownTokenSource.Cancel();
        }

        public virtual string GetConfigurationSetting(string fullName)
        {
            return ConfigurationManager.AppSettings[fullName];
        }

        public virtual NuGetService GetInstance(string name)
        {
            NuGetService ret;
            if (InstancesByName == null || !InstancesByName.TryGetValue(name, out ret))
            {
                return null;
            }
            return ret; 
        }

        public virtual T GetInstance<T>() where T : NuGetService
        {
            NuGetService ret;
            if (InstancesByType == null || !InstancesByType.TryGetValue(typeof(T), out ret))
            {
                return default(T);
            }
            return (T)ret;
        }
        
        public virtual void Initialize()
        {
            // Initialize the very very basic platform logging system (just logs service platform events to a single host-specific log file)
            // This way, if the below code fails, we can see some kind of log as to why.
            InitializeLocalLogging();

            var asmInfo = typeof(ServiceHost).GetAssemblyInfo();
            ServicePlatformEventSource.Log.EntryPoint(
                String.IsNullOrEmpty(asmInfo.SemanticVersion) ? asmInfo.FullName.Version.ToString() : asmInfo.SemanticVersion,
                asmInfo.BuildCommit);
            ServicePlatformEventSource.Log.CodeBase(typeof(ServiceHost).Assembly.CodeBase);
            
            ServicePlatformEventSource.Log.HostStarting(Description.InstanceName.ToString());
            try
            {
                // Load the services
                var dict = GetServices().ToDictionary(s => s.Name);
                Services = new ReadOnlyDictionary<string, ServiceDefinition>(dict);

                // Build the container
                _container = Compose();
                
                // Manually resolve components the host uses
                Config = _container.Resolve<ConfigurationHub>();

                // Report status
                ReportHostInitialized();

                // Start full cloud logging
                InitializeCloudLogging();
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.HostStartupFailed(Description.InstanceName.ToString(), ex);
                throw; // Don't stop the exception, we have to abort the startup process
            }
            ServicePlatformEventSource.Log.HostStarted(Description.InstanceName.ToString());
        }

        protected virtual IDisposable StartHttp(IEnumerable<NuGetHttpService> httpServices)
        {
            var urls = GetHttpUrls();

            // Set up start options
            var options = new StartOptions();
            foreach (var url in urls)
            {
                ServicePlatformEventSource.Log.BindingHttp(url);
                options.Urls.Add(url);
            }
            
            // Start the app
            return StartWebApp(httpServices, options);
        }

        public int AssignInstanceId()
        {
            // It's OK to pass volatile fields as ref to Interlocked APIs
            //  "...there are exceptions to this, such as when calling an interlocked API"
            //  from http://msdn.microsoft.com/en-us/library/4bw5ewxy.aspx
#pragma warning disable 0420 
            return Interlocked.Increment(ref _nextId) - 1;
#pragma warning restore 0420
        }

        protected virtual IContainer Compose()
        {
            ContainerBuilder builder = CreateContainerBuilder();

            // Add core module containing most of our components
            builder.RegisterModule(new NuGetCoreModule(this));

            // Add Services
            foreach (var service in Services)
            {
                builder
                    .RegisterType(service.Value.Type)
                    .Named<NuGetService>(service.Key)
                    .SingleInstance();
            }

            return builder.Build();
        }

        public Task WhenShutdown()
        {
            return _shutdownTcs.Task;
        }

        protected IDisposable StartWebApp(IEnumerable<NuGetHttpService> httpServices, StartOptions options)
        {
            return WebApp.Start(options, app => BuildApp(httpServices, app));
        }

        protected virtual IEnumerable<string> GetHttpUrls() { return Enumerable.Empty<string>(); }

        protected virtual void BuildApp(IEnumerable<NuGetHttpService> httpServices, IAppBuilder app)
        {
            // Add common host middleware in at the beginning of the pipeline
            var config = Config.GetSection<HttpConfiguration>();
            if (!String.IsNullOrEmpty(config.AdminKey))
            {
                app.UseAdminKeyAuthentication(new AdminKeyAuthenticationOptions()
                {
                    Key = config.AdminKey,
                    GrantedRole = Roles.Admin,
                    AllowInsecure = config.AllowInsecure
                });
            }

            // Add the service information middleware, which handles root requests and "/_info" requests
            app.UseNuGetServiceInformation(this);

            // Map the HTTP-compatible services to their respective roots
            foreach (var service in httpServices)
            {
                app.Map(service.BasePath, service.StartHttp);
            }
        }

        protected virtual ContainerBuilder CreateContainerBuilder()
        {
            return new ContainerBuilder();
        }

        protected virtual void ReportHostInitialized()
        {
        }

        protected virtual void Started(NuGetService instance)
        {
        }

        protected virtual void Starting(NuGetService instance)
        {
        }

        protected virtual void ConfigurationChanging()
        {
        }

        protected virtual void ConfigurationChanged()
        {
        }

        protected virtual ServiceStatus GetCurrentStatus()
        {
            return ServiceStatus.Online;
        }

        /// <summary>
        /// Initializes low-level logging of data to the local machine
        /// </summary>
        protected abstract void InitializeLocalLogging();
        /// <summary>
        /// Initializes logging of data to external sources in order to gather debugging data in the cloud
        /// </summary>
        protected abstract void InitializeCloudLogging();
        /// <summary>
        /// Gets instances of the services to host
        /// </summary>
        protected abstract IEnumerable<ServiceDefinition> GetServices();

        private async Task RunService(NuGetService service)
        {
            ServicePlatformEventSource.Log.ServiceRunning(service.ServiceName);
            try
            {
                await service.Run();
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.ServiceException(service.ServiceName, ex);
                throw;
            }
            ServicePlatformEventSource.Log.ServiceStoppedRunning(service.ServiceName);
        }

        internal async Task<NuGetService> StartService(ServiceDefinition service)
        {
            // Create a full service name
            var name = new ServiceName(Description.InstanceName, service.Name);

            // Initialize the serice, create the necessary IoC components and construct the instance.
            ServicePlatformEventSource.Log.ServiceInitializing(name);
            ILifetimeScope scope = null;
            NuGetService instance;
            try
            {
                // Resolve the service
                instance = _container.ResolveNamed<NuGetService>(
                    service.Name, 
                    new NamedParameter("name", name));

                // Construct a scope with the service
                scope = _container.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(instance)
                         .As<NuGetService>()
                         .As(service.Type);

                    // Add the container itself to the container
                    builder.Register(c => scope)
                        .As<ILifetimeScope>()
                        .SingleInstance();

                    // Add components provided by the service
                    instance.RegisterComponents(builder);
                });
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.ServiceInitializationFailed(name, ex);
                throw;
            }

            // Because of the "throw" in the catch block, we won't arrive here unless successful
            ServicePlatformEventSource.Log.ServiceInitialized(name);

            // Start the service and return it if the start succeeds.
            ServicePlatformEventSource.Log.ServiceStarting(name);
            bool result = false;
            try
            {
                Starting(instance);
                result = await instance.Start(scope);
                Started(instance);
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.ServiceStartupFailed(name, ex);
                throw;
            }

            // Because of the "throw" in the catch block, we won't arrive here unless successful
            ServicePlatformEventSource.Log.ServiceStarted(name);

            if (result)
            {
                return instance;
            }
            return null;
        }

        public virtual IEnumerable<ServiceHostInstanceInfo> GetHostInstances()
        {
            return Enumerable.Empty<ServiceHostInstanceInfo>();
        }
    }
}
