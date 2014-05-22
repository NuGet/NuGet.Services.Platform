using NuGet.Services.Hosting.Azure;

namespace NuGet.Services.Test.Echo
{
    public class Entrypoint : SingleServiceWorkerRole<EchoService>
    {
    }
}