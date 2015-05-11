// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using NuGet.Services.Configuration;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Hosting.Azure
{
    public class AzureServiceHost : ServiceHost, IDisposable
    {
        private static readonly string DefaultHostName = "nuget-local-0-unknown";
        private static readonly Regex RoleIdMatch = new Regex(@"^(.*)_IN_(?<id>\d+)$");

        private NuGetWorkerRole _worker;
        private ServiceHostDescription _description;
        private ObservableEventListener _platformEventStream;
        private List<IDisposable> _subscriptions = new List<IDisposable>();

        public override ServiceHostDescription Description
        {
            get { return _description; }
        }

        public EventLevel TraceLevel { get; private set; }

        public AzureServiceHost(NuGetWorkerRole worker)
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            _worker = worker;

            _description = new ServiceHostDescription(
                GetHostName(),
                RoleEnvironment.CurrentRoleInstance.Id);
        }

        public override Task<bool> Start()
        {
            // Load the trace level if specified
            string levelStr = GetConfigurationSetting("Trace.Level");
            EventLevel level;
            if (!String.IsNullOrEmpty(levelStr) && Enum.TryParse<EventLevel>(levelStr, true, out level))
            {
                TraceLevel = level;
            }
            else
            {
                TraceLevel = EventLevel.Warning;
            }

            RoleEnvironment.Changing += (_, __) => ConfigurationChanging();
            RoleEnvironment.Changed += (_, __) => ConfigurationChanging();
            RoleEnvironment.StatusCheck += (sender, e) =>
            {
                if (GetCurrentStatus() == ServiceStatus.Busy)
                {
                    e.SetBusy();
                }
            };

            return base.Start();
        }

        public IPEndPoint GetEndpoint(string name)
        {
            RoleInstanceEndpoint ep;
            if (!RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(name, out ep))
            {
                return null;
            }
            return ep.IPEndpoint;
        }

        public override string GetConfigurationSetting(string fullName)
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(fullName);
            }
            catch
            {
                return base.GetConfigurationSetting(fullName);
            }
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
            {
                sub.Dispose();
            }
        }

        public override IEnumerable<ServiceHostInstanceInfo> GetHostInstances()
        {
            return RoleEnvironment.CurrentRoleInstance.Role.Instances
                .Select(r => new ServiceHostInstanceInfo(
                    r.Id, 
                    r.InstanceEndpoints.ToDictionary(p => p.Key, p => 
                        p.Value.PublicIPEndpoint != null ?
                            p.Value.PublicIPEndpoint.Port.ToString() :
                            p.Value.IPEndpoint.Port.ToString())));
        }

        protected override IEnumerable<string> GetHttpUrls()
        {
            var http = GetEndpoint(Constants.HttpEndpoint);
            var https = GetEndpoint(Constants.HttpsEndpoint);
            var config = Config.GetSection<HttpConfiguration>();
            return NuGetApp.GetUrls(
                http == null ? (int?)null : http.Port,
                https == null ? (int?)null : https.Port,
                config.BasePath,
                localOnly: false);
        }

        protected override IEnumerable<ServiceDefinition> GetServices()
        {
            return _worker.GetServices();
        }

        protected override void InitializeLocalLogging()
        {
            _platformEventStream = new ObservableEventListener();
            _platformEventStream.EnableEvents(EventSources.PlatformSources, TraceLevel);

            var formatter = new EventTextFormatter(dateTimeFormat: "O");
            _platformEventStream.Subscribe(evt =>
            {
                StringBuilder b = new StringBuilder();
                using (var writer = new StringWriter(b))
                {
                    formatter.WriteEvent(evt, writer);
                }
                Trace.WriteLine(b.ToString());
            });

            try
            {
                var logsResource = RoleEnvironment.GetLocalResource("Logs");

                var logFile = Path.Combine(logsResource.RootPath, "Platform", "Platform.log.json");

                // Initialize core platform logging
                _subscriptions.Add(_platformEventStream.LogToRollingFlatFile(
                    fileName: logFile,
                    rollSizeKB: 1024,
                    timestampPattern: "yyyyMMdd-HHmmss",
                    rollFileExistsBehavior: RollFileExistsBehavior.Increment,
                    rollInterval: RollInterval.Hour,
                    formatter: new JsonEventTextFormatter(EventTextFormatting.Indented, dateTimeFormat: "O"),
                    maxArchivedFiles: 768, // We have a buffer size of 1024MB for this folder
                    isAsync: false));
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.FatalException(ex);
                throw;
            }
        }

        protected override void InitializeCloudLogging()
        {
            if (Config.Storage.Primary != null)
            {
				_subscriptions.Add(_platformEventStream.LogToWindowsAzureTable(
	                instanceName: Description.InstanceName.ToString() + "/" + Description.MachineName,
	                connectionString: Config.Storage.Primary.GetConnectionString(),
	                tableAddress: "NGPlatformTrace"));
            }
        }

        protected override void Starting(NuGetService instance)
        {
            if (Config.Storage.Primary != null)
            {
                InitializeServiceLogging(instance);
            }

            base.Starting(instance);
        }

        private void InitializeServiceLogging(NuGetService instance)
        {
            // Start logging this service's events to azure storage
            var serviceEventStream = new ObservableEventListener();
            foreach (var source in instance.GetEventSources())
            {
                serviceEventStream.EnableEvents(source, TraceLevel);
            }

            var mergedEvents = Observable.Merge(
                serviceEventStream,
                _platformEventStream.Where(evt => Equals(ServiceName.GetCurrent(), instance.ServiceName)));

            mergedEvents.LogToWindowsAzureTable(
                instanceName: instance.ServiceName.ToString(),
                connectionString: Config.Storage.Primary.GetConnectionString(),
                tableAddress: "NG" + instance.ServiceName.Name + "Trace");

            // Trace Http Requests
            var httpEventStream = new ObservableEventListener();
            httpEventStream.EnableEvents(HttpTraceEventSource.Log, EventLevel.LogAlways);
            httpEventStream
                .Where(e => Equals(ServiceName.GetCurrent(), instance.ServiceName))
                .LogToWindowsAzureTable(
                    instanceName: instance.ServiceName.ToString(),
                    connectionString: Config.Storage.Primary.GetConnectionString(),
                    tableAddress: "NG" + instance.ServiceName.Name + "Http");
        }

        private ServiceHostInstanceName GetHostName()
        {
            var hostName = GetConfigurationSetting("Host.Name");
            if(String.IsNullOrEmpty(hostName))
            {
                hostName = DefaultHostName;
            }
            ServiceHostName name = ServiceHostName.Parse(hostName);

            // Try to parse out the instance index from the role instance ID
            var match = RoleIdMatch.Match(RoleEnvironment.CurrentRoleInstance.Id);
            int instanceId;
            if (!match.Success || !Int32.TryParse(match.Groups["id"].Value, out instanceId))
            {
                instanceId = 0;
            }

            // Create the instance name
            return new ServiceHostInstanceName(name, instanceId);
        }
    }
}
