using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var args = Args.Parse<Arguments>(args);

            // Set defaults
            args.BaseDirectory = args.BaseDirectory ?? Environment.CurrentDirectory;

            //
        }
    }
}
