// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
