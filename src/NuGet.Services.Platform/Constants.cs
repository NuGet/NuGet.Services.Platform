// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    public static class Constants
    {
        public static readonly string HttpEndpoint = "http";
        public static readonly string HttpInstanceEndpoint = "http-instance";
        public static readonly string HttpsEndpoint = "https";
        public static readonly string HttpsInstanceEndpoint = "https-instance";
        public static readonly string NorthCentralUSEndpoint = @"https://ch1prod-dacsvc.azure.com/DACWebService.svc";
        public static readonly string EastUSEndpoint = @"https://bl2prod-dacsvc.azure.com/DACWebService.svc";

        public static readonly string RequestIdOwinEnvironmentKey = "nuget.requestId";
    }
}
