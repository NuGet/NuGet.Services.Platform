// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services
{
    public static class NuGetCertificateFacts
    {
        public class TheCreateOrDefaultMethod
        {
            [Theory]

            // No OU fields
            [InlineData("CN=foo")]

            // Missing OU=nuget-services trailer
            [InlineData("CN=foo, OU=azure-management")]
            public void GivenAnInvalidName_ItReturnsNull(string subjectName)
            {
                var cert = NuGetCertificate.CreateOrDefault(null, subjectName);
                Assert.Null(cert);
            }

            [Fact]
            public void GivenAValidTrailerAndNoOtherOUFields_ItReturnsNuGetCertificateWithOtherFieldsNull()
            {
                var cert = NuGetCertificate.CreateOrDefault(
                    null,
                    subjectName: "CN=name, OU=nuget-services");
                Assert.NotNull(cert);
                Assert.Null(cert.Environment);
                Assert.Null(cert.Purpose);
                Assert.Null(cert.Target);
                Assert.Empty(cert.Subtargets);
                Assert.Equal("name", cert.Name);
            }

            [Fact]
            public void GivenFirstOUFieldProvided_ItIsTheEnvironment()
            {
                var cert = NuGetCertificate.CreateOrDefault(
                    null,
                    subjectName: "CN=name, OU=env, OU=nuget-services");
                Assert.NotNull(cert);
                Assert.Equal("env", cert.Environment);
                Assert.Null(cert.Purpose);
                Assert.Null(cert.Target);
                Assert.Empty(cert.Subtargets);
                Assert.Equal("name", cert.Name);
            }

            [Fact]
            public void GivenSecondOUFieldProvided_ItIsThePurpose()
            {
                var cert = NuGetCertificate.CreateOrDefault(
                    null,
                    subjectName: "CN=name, OU=purpose, OU=env, OU=nuget-services");
                Assert.NotNull(cert);
                Assert.Equal("env", cert.Environment);
                Assert.Equal("purpose", cert.Purpose);
                Assert.Null(cert.Target);
                Assert.Empty(cert.Subtargets);
                Assert.Equal("name", cert.Name);
            }

            [Fact]
            public void GivenThirdOUFieldProvided_ItIsTheTarget()
            {
                var cert = NuGetCertificate.CreateOrDefault(
                    null,
                    subjectName: "CN=name, OU=target, OU=purpose, OU=env, OU=nuget-services");
                Assert.NotNull(cert);
                Assert.Equal("env", cert.Environment);
                Assert.Equal("purpose", cert.Purpose);
                Assert.Equal("target", cert.Target);
                Assert.Empty(cert.Subtargets);
                Assert.Equal("name", cert.Name);
            }

            [Fact]
            public void GivenFurtherOUFieldsProvided_TheyAreTheSubtargetsInReverseOrder()
            {
                var cert = NuGetCertificate.CreateOrDefault(
                    null,
                    subjectName: "CN=name, OU=grandchild, OU=child, OU=parent, OU=target, OU=purpose, OU=env, OU=nuget-services");
                Assert.NotNull(cert);
                Assert.Equal("env", cert.Environment);
                Assert.Equal("purpose", cert.Purpose);
                Assert.Equal("target", cert.Target);
                Assert.Equal(
                    new [] { "parent", "child", "grandchild" },
                    cert.Subtargets.ToArray());
                Assert.Equal("name", cert.Name);
            }
        }
    }
}
