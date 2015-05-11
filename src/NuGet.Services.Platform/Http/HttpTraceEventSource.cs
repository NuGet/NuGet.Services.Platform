// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Diagnostics.Tracing;
using System.Net;

namespace NuGet.Services.Http
{
    [EventSource(Name = "Outercurve-NuGet-Http-Trace")]
    public class HttpTraceEventSource : EventSource
    {
        public static readonly HttpTraceEventSource Log = new HttpTraceEventSource();
        private HttpTraceEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.Request,
            Message = "{0} {1} from {2} via {3} (RID:{4})")]
        public void BeginRequest(string method, string url, string referrer, string userAgent, string requestId) { WriteEvent(1, method, url, referrer, userAgent, requestId); }

        [Event(
            eventId: 2,
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.Request,
            Message = "{0} {1} Length: {2} (RID:{3})")]
        public void EndRequest(int statusCode, string url, long contentLength, string requestId) { WriteEvent(2, statusCode, url, contentLength, requestId); }
        [NonEvent]
        public void EndRequest(HttpStatusCode statusCode, string url, long contentLength, string requestId) { EndRequest((int)statusCode, url, contentLength, requestId); }

        public static class Tasks
        {
            public const EventTask Request = (EventTask)0x1;
        }
    }
}
