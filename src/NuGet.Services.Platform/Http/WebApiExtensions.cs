// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;

namespace System.Web.Http
{
    public static class WebApiExtensions
    {
        public static Uri RouteUri(this UrlHelper self, string name)
        {
            return RouteUri(self, name, new Dictionary<string, object>());
        }

        public static Uri RouteUri(this UrlHelper self, string name, Dictionary<string, object> routeValues)
        {
            return new Uri(self.Request.RequestUri, self.Route(name, routeValues));
        }

        public static Uri RouteUri(this UrlHelper self, string name, object routeValues)
        {
            return new Uri(self.Request.RequestUri, self.Route(name, routeValues));
        }
    }
}
