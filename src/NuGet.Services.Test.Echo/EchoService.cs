// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;
using Owin;

namespace NuGet.Services.Test.Echo
{
    public class EchoService : NuGetHttpService
    {
        private static readonly PathString _path = new PathString("/echo");
        public override PathString BasePath
        {
            get { return _path; }
        }

        public EchoService(ServiceName name, ServiceHost host) : base(name, host) { }

        public override IEnumerable<EventSource> GetEventSources()
        {
            yield return EchoServiceEventSource.Log;
        }

        protected override void Configure(IAppBuilder app)
        {
            app.Map(new PathString("/adminsOnly"), a =>
            {
                a.Use(async (ctx, next) =>
                {
                    if (ctx.Request.User == null)
                    {
                        ctx.Authentication.Challenge();
                    }
                    else
                    {
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync("Welcome, admin!");
                    }
                });
            });

            app.Map(new PathString("/adminsGetExtra"), a =>
            {
                a.Use(async (ctx, next) =>
                {
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("Welcome!");
                    if (ctx.Request.User != null)
                    {
                        await ctx.Response.WriteAsync(" You are logged in!");
                        if (ctx.Request.User.IsInRole(Roles.Admin))
                        {
                            await ctx.Response.WriteAsync(" And you are an admin!");
                        }
                    }
                });
            });

            app.Map(new PathString("/echo"), a =>
            {
                a.Use(async (ctx, next) =>
                {
                    var message = ctx.Request.Query.Get("message");
                    if (String.IsNullOrEmpty(message))
                    {
                        message = "Put something in the 'message' query string parameter and I'll repeat it!";
                    }
                    else
                    {
                        EchoServiceEventSource.Log.Echoing(message);
                    }
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync(message);
                });
            });

            app.Map(new PathString("/throw"), a =>
            {
                a.Use(async (ctx, next) =>
                {
                    await ctx.Response.WriteAsync("Throwing!");
                    throw new Exception("Throw me a frickin' bone!");
                });
            });

            app.Map(new PathString(""), a =>
            {
                a.Use(async (ctx, next) =>
                {
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(@"{ 'operations': ['/adminsOnly', '/adminsGetExtra', '/echo'] }");
                });
            });
        }
    }

    [EventSource(Name = "Outercurve-NuGet-Services-Echo")]
    public class EchoServiceEventSource : EventSource
    {
        public static readonly EchoServiceEventSource Log = new EchoServiceEventSource();
        private EchoServiceEventSource() { }

        [Event(eventId: 1, Message="Echoing '{0}'")]
        public void Echoing(string message) { WriteEvent(1, message); }
    }
}
