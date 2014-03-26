using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.WindowsAzure.ServiceRuntime;
using NuGet.Services.Configuration;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Hosting.Azure
{
    public class AzureServiceHost : ServiceHost, IDisposable
    {
        private static readonly Regex RoleIdMatch = new Regex(@"^(.*)_IN_(?<id>\d+)$");

        private NuGetWorkerRole _worker;
        private ServiceHostDescription _description;
        private ObservableEventListener _platformEventStream;
        private List<IDisposable> _subscriptions = new List<IDisposable>();

        public override ServiceHostDescription Description
        {
            get { return _description; }
        }

        public AzureServiceHost(NuGetWorkerRole worker)
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            _worker = worker;

            _description = new ServiceHostDescription(
                GetHostName(),
                RoleEnvironment.CurrentRoleInstance.Id);
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

        protected override IEnumerable<string> GetHttpUrls()
        {
            var http = GetEndpoint(Constants.HttpEndpoint);
            var https = GetEndpoint(Constants.HttpsEndpoint);
            var config = Config.GetSection<HttpConfiguration>();
            return NuGetApp.GetUrls(
                http == null ? (int?)null : http.Port,
                https == null ? (int?)null : https.Port,
                config.BasePath);
        }

        protected override IEnumerable<ServiceDefinition> GetServices()
        {
            return _worker.GetServices();
        }

        protected override void InitializeLocalLogging()
        {
            _platformEventStream = new ObservableEventListener();
            _platformEventStream.EnableEvents(SemanticLoggingEventSource.Log, EventLevel.Informational);
            _platformEventStream.EnableEvents(ServicePlatformEventSource.Log, EventLevel.LogAlways);

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
            _subscriptions.Add(_platformEventStream.LogToWindowsAzureTable(
                instanceName: Description.InstanceName.ToString() + "/" + Description.MachineName,
                connectionString: Storage.Primary.ConnectionString,
                tableAddress: Storage.Primary.Tables.GetTableFullName("PlatformTrace")));
        }

        private ServiceHostInstanceName GetHostName()
        {
            var hostName = GetConfigurationSetting("Host.Name");
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
