// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Hosting
{
    public interface IApplicationHost
    {
        // This interface should rarely or never be modified. It defines
        // how a Host App Domain communicates with the target App Domain
        // If this is changed, old Host App Domains (nuhost.exe versions)
        // may fail to bootstrap the Platform.

        void Run(NuGetDomainStartOptions options);
        Task RunAsync(NuGetDomainStartOptions options);
    }
}
