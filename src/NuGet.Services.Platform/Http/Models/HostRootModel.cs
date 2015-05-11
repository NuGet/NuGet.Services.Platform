// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Http.Models
{
    public class HostRootModel
    {
        public Uri Host { get; set; }
        public object Api { get; set; }
    }
}
