using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace NuGet.Services.ServiceModel
{
    public class ServiceHostInstanceInfo
    {
        public string Name { get; private set; }
        public IDictionary<string, string> Endpoints { get; private set; }

        public ServiceHostInstanceInfo(string id, Dictionary<string, string> endpoints)
        {
            Name = id;
            Endpoints = endpoints;
        }
    }
}
