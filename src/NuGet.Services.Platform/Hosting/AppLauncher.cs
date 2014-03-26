using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Hosting
{
    public static class AppLauncher
    {
        public static Task<NuGetApp> LaunchInProcess(NuGetStartOptions options)
        {
            return NuGetApp.Start(options);
        }

        public static Task<NuGetApp> LaunchInNewAppDomain(string baseDirectory, NuGetStartOptions options)
        {
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = baseDirectory;
            var domain = AppDomain.CreateDomain("NuGetApp", null, setup);

            return booter.Boot(options);
        }

        private class AppBootloader
        {
            public Task<NuGetApp> Boot(NuGetStartOptions options)
            {
                return NuGetApp.Start(options);
            }
        }
    }
}
