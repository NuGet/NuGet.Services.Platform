﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
    public static class EventListenerExtensions
    {
        public static void EnableEvents(this EventListener self, IEnumerable<EventSource> sources, EventLevel level)
        {
            foreach (var source in sources)
            {
                self.EnableEvents(source, level);
            }
        }
    }
}
