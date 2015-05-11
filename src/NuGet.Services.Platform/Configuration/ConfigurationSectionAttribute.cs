// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Configuration
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConfigurationSectionAttribute : Attribute
    {
        public string Name { get; private set; }
        public ConfigurationSectionAttribute(string name)
        {
            Name = name;
        }
    }
}
