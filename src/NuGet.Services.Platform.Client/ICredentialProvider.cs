// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    public interface ICredentialProvider
    {
        bool ApplyLocalCacheCredentials(HttpRequestMessage request);
        Task<bool> ApplyCredentials(HttpResponseMessage unauthorizedResponse, HttpRequestMessage request);
    }
}
