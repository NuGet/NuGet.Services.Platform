using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace NuGet.Services.Hosting
{
    public class NuGetDomainAgent : MarshalByRefObject
    {
        private NuGetApp _app;

        public event EventHandler<LogEventEntry> EventLogged;
        public event EventHandler<bool> ApplicationStarted;
        public event EventHandler<Exception> ApplicationShutdown;

        public void Start(NuGetStartOptions options)
        {
            _app = NuGetApp.Create(options);
            _app.EventStream.Subscribe(e => OnEvent(e));
            _app.Start().ContinueWith(t => { OnStarted(t.IsCompleted ? t.Result : false); return t; });
        }

        private void OnEvent(EventEntry e)
        {
            var logEvent = new LogEventEntry()
            {
                EventId = e.EventId,
                FormattedMessage = e.FormattedMessage,
                Payload = e.Payload.ToArray(),
                ProviderId = e.ProviderId,
                Timestamp = e.Timestamp,
                Schema = new LogEventSchema()
                {
                    EventName = e.Schema.EventName,
                    Id = e.Schema.Id,
                    Keywords = e.Schema.Keywords,
                    KeywordsDescription = e.Schema.KeywordsDescription,
                    Level = e.Schema.Level,
                    Opcode = e.Schema.Opcode,
                    OpcodeName = e.Schema.OpcodeName,
                    Payload = e.Schema.Payload,
                    ProviderId = e.Schema.ProviderId,
                    ProviderName = e.Schema.ProviderName,
                    Task = e.Schema.Task,
                    TaskName = e.Schema.TaskName,
                    Version = e.Schema.Version
                }
            };
            var handler = EventLogged;
            if (handler != null)
            {
                handler(this, logEvent);
            }
        }

        public void Shutdown()
        {
            _app.Shutdown();
        }

        public void Run()
        {
            _app.Run().ContinueWith(t => OnApplicationShutdown(t));
        }

        private void OnStarted(bool result)
        {
            var handler = ApplicationStarted;
            if (handler != null)
            {
                handler(this, result);
            }
        }

        private Task OnApplicationShutdown(Task t)
        {
            Exception result = null;
            if(t.IsFaulted) {
                result = t.Exception;
            }
            var handler = ApplicationShutdown;
            if (handler != null)
            {
                handler(this, result);
            }
            return t;
        }
    }
}
