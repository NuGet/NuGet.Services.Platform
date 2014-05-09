using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    public static class EventSources
    {
        private static readonly Lazy<IEnumerable<EventSource>> _platformSources = new Lazy<IEnumerable<EventSource>>(FindPlatformSources);
        private static readonly Lazy<IEnumerable<EventSource>> _nugetSources = new Lazy<IEnumerable<EventSource>>(FindAllNuGetSources);

        public static IEnumerable<EventSource> PlatformSources { get { return _platformSources.Value; } }
        public static IEnumerable<EventSource> AllNuGetSources { get { return _nugetSources.Value; } }

        private static IEnumerable<EventSource> FindPlatformSources()
        {
            return typeof(EventSources)
                .Assembly
                .GetExportedTypes()
                .Where(t => typeof(EventSource).IsAssignableFrom(t))
                .Select(t => t.GetField("Log", BindingFlags.Public | BindingFlags.Static))
                .Where(f => f != null && typeof(EventSource).IsAssignableFrom(f.FieldType))
                .Select(f => (EventSource)f.GetValue(null));
        }

        private static IEnumerable<EventSource> FindAllNuGetSources()
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("NuGet.", StringComparison.OrdinalIgnoreCase))
                .SelectMany(a => a
                    .GetExportedTypes()
                    .Where(t => typeof(EventSource).IsAssignableFrom(t))
                    .Select(t => t.GetField("Log", BindingFlags.Public | BindingFlags.Static))
                    .Where(f => f != null && typeof(EventSource).IsAssignableFrom(f.FieldType))
                    .Select(f => (EventSource)f.GetValue(null)));
        }
    }
}
