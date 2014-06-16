using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Work.Azure;

namespace NuGet.Services
{
    public static class EventSources
    {
        public static IEnumerable<EventSource> PlatformSources
        {
            get
            {
                yield return ServicePlatformEventSource.Log;
                yield return AzureHubEventSource.Log;
            }
        }
    }
}
