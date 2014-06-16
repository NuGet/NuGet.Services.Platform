using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services;

namespace Microsoft.Owin
{
    public static class OwinExtensions
    {
        public static string GetRequestId(this IOwinContext self)
        {
            return self.Get<string>(Constants.RequestIdOwinEnvironmentKey);
        }
    }
}
