// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Client
{
    public class ServiceResponse : ServiceResponse<string>
    {
        public ServiceResponse(HttpResponseMessage httpResponse) : base(httpResponse) { }
        
        public override Task<string> ReadContent()
        {
            return HttpResponse.Content.ReadAsStringAsync();
        }
    }

    public class ServiceResponse<T>
    {
        private Func<Task<T>> _reader;

        public HttpResponseMessage HttpResponse { get; private set; }
        public HttpStatusCode StatusCode { get { return HttpResponse.StatusCode; } }
        public bool IsSuccessStatusCode { get { return HttpResponse.IsSuccessStatusCode; } }
        public string ReasonPhrase { get { return HttpResponse.ReasonPhrase; } }

        public ServiceResponse(HttpResponseMessage httpResponse)
            : this(httpResponse, () => httpResponse.Content.ReadAsAsync<T>())
        {
        }

        public ServiceResponse(HttpResponseMessage httpResponse, Func<Task<T>> reader)
        {
            HttpResponse = httpResponse;
            _reader = reader;
        }

        public virtual Task<T> ReadContent()
        {
            return _reader();
        }
    }

    public static class HttpResponseExtensions
    {
        public static ServiceResponse AsServiceResponse(this HttpResponseMessage self)
        {
            return new ServiceResponse(self);
        }

        public static ServiceResponse<T> AsServiceResponse<T>(this HttpResponseMessage self)
        {
            return new ServiceResponse<T>(self);
        }

        public static async Task<ServiceResponse> AsServiceResponse(this Task<HttpResponseMessage> self)
        {
            return new ServiceResponse(await self);
        }

        public static async Task<ServiceResponse<T>> AsServiceResponse<T>(this Task<HttpResponseMessage> self)
        {
            return new ServiceResponse<T>(await self);
        }
    }
}
