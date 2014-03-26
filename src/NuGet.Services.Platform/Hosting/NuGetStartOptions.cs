using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Owin.Hosting;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Hosting
{
    [Serializable]
    public class NuGetStartOptions : StartOptions
    {
        public IList<ServiceDefinition> Services { get; private set; }
        public ServiceHostDescription AppDescription { get; set; }
        public Func<string, string> ConfigurationProvider { get; set; }
        public IDictionary<string, string> Configuration { get; set; }

        public NuGetStartOptions()
            : base()
        {
            Services = new List<ServiceDefinition>();
        }
    }
}
