using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Hosting
{
    public class LocalServiceHost : ServiceHost
    {
        private NuGetStartOptions _options;
        private Func<string, string> _configProvider;
        protected ObservableEventListener EventListener { get; private set; }

        public IObservable<EventEntry> EventStream { get; private set; }
        public override ServiceHostDescription Description { get { return _options.AppDescription; } }

        public LocalServiceHost(NuGetStartOptions options)
        {
            _options = options;

            if (_options.ConfigurationProvider != null)
            {
                _configProvider = _options.ConfigurationProvider;
            }
            else if (_options.Configuration != null)
            {
                _configProvider = s => _options.Configuration[s];
            }
            else
            {
                _configProvider = ConfigurationManager.AppSettings.Get;
            }
        }

        protected override void InitializeLocalLogging()
        {
            EventStream = EventListener = new ObservableEventListener();
            EventListener.EnableEvents(SemanticLoggingEventSource.Log, EventLevel.Informational);
            EventListener.EnableEvents(ServicePlatformEventSource.Log, EventLevel.LogAlways);
        }

        protected override void InitializeCloudLogging()
        {
        }

        protected override IEnumerable<ServiceDefinition> GetServices()
        {
            return _options.Services;
        }

        protected override void Starting(NuGetService instance)
        {
            base.Starting(instance);

            foreach (var eventSource in instance.GetEventSources())
            {
                EventListener.EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

        public override string GetConfigurationSetting(string fullName)
        {
            return _configProvider(fullName);
        }
        
        protected override IDisposable StartHttp(IEnumerable<NuGetHttpService> httpServices)
        {
            return StartWebApp(httpServices, _options);
        }

        private static ServiceHostDescription GetServiceDescription(string hostName)
        {
            return new ServiceHostDescription(
                ServiceHostInstanceName.Parse("nuget-local-0-" + hostName + "_IN0"),
                Environment.MachineName);
        }
    }
}
