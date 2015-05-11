// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NuHost
{
    public class NuHostConfig
    {
        public static NuHostConfig Default = new NuHostConfig();

        public string ClrConfigFile { get; set; }

        private NuHostConfig() { }

        public static NuHostConfig Load(string configFile)
        {
            return JsonConvert.DeserializeObject<NuHostConfig>(
                File.ReadAllText(configFile),
                new JsonSerializerSettings() {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}
