// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
        private IEnumerable<ServiceDefinition> _services;
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
                _configProvider = s => {
                    string val;
                    if (!_options.Configuration.TryGetValue(s, out val))
                    {
                        return null;
                    }
                    return val;
                };
            }
            else
            {
                _configProvider = ConfigurationManager.AppSettings.Get;
            }

            // Resolve services
            var allServices = ServiceDefinition.GetAllServicesInAppDomain();
            if (options.Services.Any())
            {
                _services = options.Services.Select(s => ResolveService(s, allServices)).Where(s => s != null);
            }
            else
            {
                _services = allServices.Values;
            }

            EventStream = EventListener = new ObservableEventListener();
        }

        protected override void InitializeLocalLogging()
        {
            EventListener.EnableEvents(SemanticLoggingEventSource.Log, EventLevel.Informational);
            EventListener.EnableEvents(ServicePlatformEventSource.Log, EventLevel.LogAlways);
        }

        protected override void InitializeCloudLogging()
        {
        }

        protected override IEnumerable<ServiceDefinition> GetServices()
        {
            return _services;
        }

        protected override void Starting(NuGetService instance)
        {
            foreach (var source in instance.GetEventSources())
            {
                EventListener.EnableEvents(source, EventLevel.Informational);
            }

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
            foreach (var url in _options.Urls)
            {
                ServicePlatformEventSource.Log.BindingHttp(url);
            }
            return StartWebApp(httpServices, _options);
        }

        private static ServiceHostDescription GetServiceDescription(string hostName)
        {
            return new ServiceHostDescription(
                ServiceHostInstanceName.Parse("nuget-local-0-" + hostName + "_IN0"),
                Environment.MachineName);
        }

        private static ServiceDefinition ResolveService(string name, IDictionary<string, ServiceDefinition> allServices)
        {
            ServiceDefinition defn;
            if (!allServices.TryGetValue(name, out defn))
            {
                return null;
            }
            return defn;
        }
    }
}
