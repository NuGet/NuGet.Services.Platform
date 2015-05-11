// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace NuGet.Services.Configuration
{
    public class HttpConfiguration
    {
        [Description("The base path to host the application at")]
        public string BasePath { get; set; }

        [Description("The admin password used by external services")]
        public string AdminKey { get; set; }

        [Description("Set this to allow insecure (HTTP) authenticated requests")]
        public bool AllowInsecure { get; set; }
    }
}
