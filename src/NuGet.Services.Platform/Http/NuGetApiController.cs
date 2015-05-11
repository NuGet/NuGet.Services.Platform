// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Services.ServiceModel;
using System.Net.Http;
using Microsoft.Owin;

namespace NuGet.Services.Http
{
    public abstract class NuGetApiController : ApiController
    {
        public ServiceHost Host { get; set; }
        public NuGetApiService Service { get; set; }
        public ILifetimeScope Container { get; set; }

        public string RequestId
        {
            get
            {
                var ctx = Request.GetOwinContext();
                return ctx.GetRequestId();
            }
        }

        public NuGetApiController()
        {
        }

        protected TransferBlobResult TransferBlob(ICloudBlob blob)
        {
            return new TransferBlobResult(blob);
        }

        protected Task<TransferBlobResult> TransferBlob(string blobUri)
        {
            return TransferBlob(new Uri(blobUri));
        }

        protected async Task<TransferBlobResult> TransferBlob(Uri blobUri)
        {
            var blobClient = Service.Configuration.Storage.Primary.CreateCloudBlobClient();
            var blob = await blobClient.GetBlobReferenceFromServerAsync(blobUri);
            return TransferBlob(blob);
        }
    }
}
