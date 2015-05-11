// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services;

namespace System
{
    public static class TypeExtensions
    {
        public static AssemblyInformation GetAssemblyInfo(this Type self)
        {
            return GetAssemblyInfo(self.Assembly);
        }

        public static AssemblyInformation GetAssemblyInfo(this Assembly self)
        {
            var metadata = self.GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToDictionary(m => m.Key, m => m.Value);
            return new AssemblyInformation(
                self.GetName(),
                GetOrDefault(metadata, "Branch"),
                GetOrDefault(metadata, "CommitId"),
                GetOrDefault(metadata, "BuildDateUtc"),
                GetOrDefault(metadata, "RepositoryUrl"),
                GetSemanticVersion(self));
        }

        public static string GetSemanticVersion(this Type self)
        {
            return GetSemanticVersion(self.Assembly);
        }

        public static string GetSemanticVersion(this Assembly self)
        {
            var attr = self.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attr == null)
            {
                return null;
            }
            return attr.InformationalVersion;
        }

        private static string GetOrDefault(Dictionary<string, string> dict, string key)
        {
            string val;
            if (!dict.TryGetValue(key, out val))
            {
                return String.Empty;
            }
            return val;
        }
    }
}
