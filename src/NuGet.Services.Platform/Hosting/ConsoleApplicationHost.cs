using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Hosting
{
    public class ConsoleApplicationHost : MarshalByRefObject, IApplicationHost
    {
        public void Run(NuGetStartOptions options)
        {
            RunAsync(options).Wait();
        }

        public async Task RunAsync(NuGetStartOptions options)
        {
            var app = NuGetApp.Create(options);
            Console.WriteLine(Strings.ConsoleApplicationHost_Running);
            app.EventStream.Subscribe(ev =>
            {
                Console.WriteLine("[{0}]({1:000}) {2}", ev.Schema.ProviderName, ev.EventId, ev.FormattedMessage);
            });
            if (!await app.Start())
            {
                Console.WriteLine(Strings.ConsoleApplicationHost_FailedToStart);
            }
            else
            {
                Console.CancelKeyPress += (sender, args) =>
                {
                    app.Shutdown();
                };
                await app.Run();
            }
        }
    }
}
