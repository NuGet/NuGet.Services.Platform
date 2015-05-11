// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.ServiceModel
{
    [Serializable]
    public class ServiceHostDescription
    {
        public ServiceHostInstanceName InstanceName { get; private set; }
        public string MachineName { get; private set; }

        public ServiceHostDescription(ServiceHostInstanceName instanceName, string machineName)
        {
            InstanceName = instanceName;
            MachineName = machineName;
        }
    }
}
