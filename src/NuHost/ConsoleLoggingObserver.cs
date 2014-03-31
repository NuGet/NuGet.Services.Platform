using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace NuHost
{
    class ConsoleLoggingObserver : IObserver<EventEntry>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(EventEntry value)
        {
            Console.WriteLine("[{0}]({1:000}) {2}", value.ProviderId.ToString("N"), value.EventId, value.FormattedMessage);
        }
    }
}
