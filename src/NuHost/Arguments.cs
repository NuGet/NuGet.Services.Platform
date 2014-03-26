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
        [ArgDescription("The base directory in which to locate services")]
        public string BaseDirectory { get; set; }

        [ArgPosition(0)]
        [ArgShortcut("-s")]
        [ArgDescription("The services to execute. Leave blank to run all services.")]
        public string[] Services { get; set; }
    }
}
