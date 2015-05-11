// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Hosting
{
    [Serializable]
    public class LogEventEntry
    {
        public int EventId { get; set; }
        public string FormattedMessage { get; set; }
        public object[] Payload { get; set; }
        public Guid ProviderId { get; set; }
        public LogEventSchema Schema { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    [Serializable]
    public class LogEventSchema
    {
        public string EventName { get; set; }
        public int Id { get; set; }
        public EventKeywords Keywords { get; set; }
        public string KeywordsDescription { get; set; }
        public EventLevel Level { get; set; }
        public EventOpcode Opcode { get; set; }
        public string OpcodeName { get; set; }
        public string[] Payload { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; }
        public EventTask Task { get; set; }
        public string TaskName { get; set; }
        public int Version { get; set; }
    }
}
