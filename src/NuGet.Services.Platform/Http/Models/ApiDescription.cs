// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Http.Models
{
    public class ApiDescription
    {
        public Uri Self { get; private set; }
        public string Host { get; set; }
        public IDictionary<string, Uri> Services { get; private set; }
        public IDictionary<string, ServiceVersionInformation> Versions { get; private set; } 

        public ApiDescription(Uri baseUrl, IEnumerable<NuGetHttpService> httpServices, IEnumerable<NuGetService> allServices)
        {
            Self = baseUrl;
            Services = httpServices.ToDictionary(
                service => service.ServiceName.Name.ToLowerInvariant(),
                service => new Uri(baseUrl, service.BasePath.ToUriComponent()),
                StringComparer.OrdinalIgnoreCase);
            Versions = allServices.ToDictionary(
                service => service.ServiceName.Name.ToLowerInvariant(),
                service => CreateVersionInfo(service),
                StringComparer.OrdinalIgnoreCase);
        }

        private ServiceVersionInformation CreateVersionInfo(NuGetService service)
        {
            var asmName = service.GetType().Assembly.GetName();
            var asmInfo = service.GetType().GetAssemblyInfo();
            return new ServiceVersionInformation()
            {
                Version = asmInfo.SemanticVersion,
                Branch = asmInfo.BuildBranch,
                Commit = asmInfo.BuildCommit,
                BuildDateUtc = asmInfo.BuildDate.UtcDateTime
            };
        }
    }

    public class ServiceVersionInformation
    {
        public string Name { get; set; }
        public string Branch { get; set; }
        public string Commit { get; set; }
        public DateTime BuildDateUtc { get; set; }
        public string Version { get; set; }
    }
}
