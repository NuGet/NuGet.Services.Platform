// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
        public IEnumerable<string> Services { get; set; }
        public ServiceHostDescription AppDescription { get; set; }
        public Func<string, string> ConfigurationProvider { get; set; }
        public IDictionary<string, string> Configuration { get; set; }

        public NuGetStartOptions()
            : base()
        {
            Services = new List<string>();
            Configuration = new Dictionary<string, string>();
        }
    }

    [Serializable]
    public class NuGetDomainStartOptions : NuGetStartOptions
    {
        public string HostApplicationBase { get; set; }
        public string ApplicationBase { get; set; }
    }
}
