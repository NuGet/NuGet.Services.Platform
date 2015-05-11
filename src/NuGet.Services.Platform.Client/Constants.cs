// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    public static class BlobContainerNames
    {
        public static readonly string LegacyPackages = "packages";
        public static readonly string LegacyStats = "stats";

        public static readonly string BacpacFiles = "bacpac-files";
        public static readonly string Backups = "ng-backups";
    }

    public static class MimeTypes
    {
        public static readonly string Json = "application/json";
    }
}