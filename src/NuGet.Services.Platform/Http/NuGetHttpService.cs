// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Owin;
using NuGet.Services.ServiceModel;
using Owin;

namespace NuGet.Services.Http
{
    public abstract class NuGetHttpService : NuGetService
    {
        private readonly PathString _defaultPathString;

        public virtual PathString BasePath
        {
            get { return _defaultPathString; }
        }

        protected NuGetHttpService(ServiceName name, ServiceHost host) : base(name, host) {
            _defaultPathString = new PathString("/" + name.Name.ToLowerInvariant());
        }

        public virtual void StartHttp(IAppBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                // Set the call context
                ServiceName.SetCurrent(ServiceName);
                Heartbeat(); // Fire a heartbeat, we're still alive!

                // Check for an existing Request ID and if there isn't one, generate one.
                string rid = ctx.Request.Headers.Get("X-ARR-LOG-ID");
                if(String.IsNullOrEmpty(rid))
                {
                    rid = Guid.NewGuid().ToString();
                }
                ctx.Set(Constants.RequestIdOwinEnvironmentKey, rid);

                HttpTraceEventSource.Log.BeginRequest(
                    ctx.Request.Method,
                    ctx.Request.Uri.AbsoluteUri,
                    ctx.Request.Headers.Get("Referer") ?? String.Empty, /* sic, see http://en.wikipedia.org/wiki/List_of_HTTP_header_fields */
                    ctx.Request.Headers.Get("User-Agent") ?? String.Empty,
                    rid);
                bool error = false;
                try
                {
                    try
                    {
                        await next();
                    }
                    catch (Exception ex)
                    {
                        ServicePlatformEventSource.Log.HttpException(ctx.Request.Uri.AbsoluteUri, ex);

                        // Don't allow the exception to bring down the service.
                        error = true;
                    }

                    if(error)
                    {
                        ctx.Response.StatusCode = 500;
                        await ctx.Response.WriteAsync(Strings.NuGetHttpService_UnknownError);
                    }
                }
                finally
                {
                    HttpTraceEventSource.Log.EndRequest(
                        ctx.Response.StatusCode,
                        ctx.Request.Uri.AbsoluteUri,
                        ctx.Response.ContentLength ?? 0,
                        rid);
                }
            });
            Configure(app);
        }

        protected override Task OnRun()
        {
            return Host.WhenShutdown();
        }

        protected abstract void Configure(IAppBuilder app);

        public override void RegisterComponents(ContainerBuilder builder)
        {
            base.RegisterComponents(builder);

            builder.RegisterInstance(this).As<NuGetHttpService>();
        }
    }
}
