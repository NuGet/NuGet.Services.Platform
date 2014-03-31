using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace NuHost
{
    public class Arguments
    {
        [ArgShortcut("-b")]
        [ArgDescription("(OPTIONAL) The base directory in which to locate services. Leave blank to use the current directory")]
        public string BaseDirectory { get; set; }

        [ArgPosition(0)]
        [ArgShortcut("-s")]
        [ArgDescription("(OPTIONAL) The services to execute. Leave blank to run all services.")]
        public string[] Services { get; set; }

        [ArgShortcut("-p")]
        [ArgDescription("(OPTIONAL) The HTTP Port to use. Leave blank to disable HTTP")]
        public int? HttpPort { get; set; }

        [ArgShortcut("-ps")]
        [ArgDescription("(OPTIONAL) The HTTPS Port to use. Leave blank to disable HTTPS")]
        public int? HttpsPort { get; set; }

        [ArgShortcut("-u")]
        [ArgDescription("(OPTIONAL) The URLs to bind HTTP services to.")]
        public string[] Urls { get; set; }

        [ArgShortcut("-path")]
        [ArgDescription("(OPTIONAL) The root path to use for the service.")]
        public string BasePath { get; set; }
    }
}
