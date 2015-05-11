// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace NuGet.Services.Http.Authentication
{
    public class AdminKeyAuthenticationHandler : AuthenticationHandler<AdminKeyAuthenticationOptions>
    {
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            // Based on http://lbadri.wordpress.com/2013/07/13/basic-authentication-with-asp-net-web-api-using-owin-middleware/

            if (!Context.Request.IsSecure && !Options.AllowInsecure)
            {
                // No authentication on insecure links
                return Task.FromResult<AuthenticationTicket>(null);
            }

            var header = Context.Request.Headers.Get("Authorization");

            if (!String.IsNullOrWhiteSpace(header))
            {
                var authHeader = AuthenticationHeaderValue.Parse(header);

                if ("Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    string parameter = 
                        Encoding.UTF8.GetString(
                            Convert.FromBase64String(
                                authHeader.Parameter));
                    var parts = parameter.Split(':');

                    string userName = parts[0];
                    string password = parts[1];

                    if (String.Equals(password, Options.Key)) // Just a dumb check
                    {
                        var identity = new ClaimsIdentity(GenerateClaims(userName), "AdminKey");

                        return Task.FromResult(new AuthenticationTicket(identity, new AuthenticationProperties()));
                    }
                }
            }
            return Task.FromResult<AuthenticationTicket>(null);
        }

        protected IEnumerable<Claim> GenerateClaims(string userName)
        {
            yield return new Claim(ClaimTypes.NameIdentifier, Options.GrantedUserName ?? userName);
            if (!String.IsNullOrEmpty(Options.GrantedRole))
            {
                yield return new Claim(ClaimTypes.Role, Options.GrantedRole);
            }
        }

        protected override async Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
            {
                return;
            }

            AuthenticationResponseChallenge challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);
            if (challenge != null)
            {
                if (Context.Request.IsSecure || Options.AllowInsecure)
                {
                    // Only challenge if secure
                    Context.Response.Headers.Add("WWW-Authenticate", new[] { "Basic realm =\"" + Context.Request.Uri.Host + "\"" });
                }
                else if(!Context.Get<bool>("NuGet.Auth.AdminKey.WroteHttpFailure"))
                {
                    Context.Set<bool>("NuGet.Auth.AdminKey.WroteHttpFailure", true);
                    Context.Response.StatusCode = 403;
                    Context.Response.ContentType = "text/plain";
                    Context.Response.ReasonPhrase = Strings.AdminKeyAuthenticationHandler_CannotAuthenticateOverHttp;
                    await Context.Response.WriteAsync(Strings.AdminKeyAuthenticationHandler_CannotAuthenticateOverHttp);
                }
            }
            await base.ApplyResponseChallengeAsync();
        }
    }
}
