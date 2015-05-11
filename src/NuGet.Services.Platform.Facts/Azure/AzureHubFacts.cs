// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NuGet.Services.Configuration;
using NuGet.Services.Work.Azure;
using Xunit;

namespace NuGet.Services.Azure
{
    public class AzureHubFacts
    {
        public class TheConstructor
        {
            [Fact]
            public void GivenCertificateWithoutTarget_ItThrowsConfigurationException()
            {
                // Arrange
                var cert = new NuGetCertificate(
                    name: "name",
                    purpose: CommonCertificatePurposes.AzureManagement,
                    environment: "test",
                    target: null,
                    x509: null);
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                certs
                    .Setup(c => c.GetCertificateFromConfigSetting(AzureHub.ConfigSetting, StoreName.My, StoreLocation.LocalMachine))
                    .Returns(cert);

                // Act/Assert
                var ex = Assert.Throws<ConfigurationException>(() => new AzureHub(certs.Object));
                Assert.Equal(Strings.AzureHub_MissingSubscription, ex.Message);
            }

            [Fact]
            public void GivenCertificateWithInvalidTarget_ItThrowsConfigurationException()
            {
                // Arrange
                var cert = new NuGetCertificate(
                    name: "name",
                    purpose: CommonCertificatePurposes.AzureManagement,
                    environment: "test",
                    target: "nope! Not valid!",
                    x509: null);
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                certs
                    .Setup(c => c.GetCertificateFromConfigSetting(AzureHub.ConfigSetting, StoreName.My, StoreLocation.LocalMachine))
                    .Returns(cert);

                // Act/Assert
                var ex = Assert.Throws<ConfigurationException>(() => new AzureHub(certs.Object));
                Assert.Equal(Strings.AzureHub_MissingSubscription, ex.Message);
            }

            [Theory]
            [InlineData(StoreLocation.LocalMachine)]
            [InlineData(StoreLocation.CurrentUser)]
            public void GivenCertificateSpecifiedInConfig_ItSetsPropertiesFromIt(StoreLocation location)
            {
                // Arrange
                var cert = new NuGetCertificate(
                    name: "name",
                    purpose: CommonCertificatePurposes.AzureManagement,
                    environment: "test",
                    target: "Foo[abc]",
                    x509: null);
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                certs
                    .Setup(c => c.GetCertificateFromConfigSetting(AzureHub.ConfigSetting, StoreName.My, location))
                    .Returns(cert);

                // Act
                var azure = new AzureHub(certs.Object);

                // Assert
                Assert.Same(cert, azure.ManagementCertificate);
                Assert.Equal("Foo", azure.SubscriptionName);
                Assert.Equal("abc", azure.SubscriptionId);
            }

            [Theory]
            [InlineData(StoreLocation.LocalMachine)]
            [InlineData(StoreLocation.CurrentUser)]
            public void GivenCertificateSpecifiedByPurpose_ItSetsPropertiesFromIt(StoreLocation location)
            {
                // Arrange
                var cert = new NuGetCertificate(
                    name: "name",
                    purpose: CommonCertificatePurposes.AzureManagement,
                    environment: "test",
                    target: "Foo[abc]",
                    x509: null);
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                certs
                    .Setup(c => c.GetCertificatesByPurpose(CommonCertificatePurposes.AzureManagement, StoreName.My, location))
                    .Returns(new [] { cert });

                // Act
                var azure = new AzureHub(certs.Object);

                // Assert
                Assert.Same(cert, azure.ManagementCertificate);
                Assert.Equal("Foo", azure.SubscriptionName);
                Assert.Equal("abc", azure.SubscriptionId);
            }

            [Theory]
            [InlineData(StoreLocation.LocalMachine)]
            [InlineData(StoreLocation.CurrentUser)]
            public void GivenMultipleCertificatesSpecifiedByPurpose_ItSelectsFirstOne(StoreLocation location)
            {
                // Arrange
                var firstCert = new NuGetCertificate(
                    name: "first",
                    purpose: CommonCertificatePurposes.AzureManagement,
                    environment: "test",
                    target: "First[abc]",
                    x509: null);
                var secondCert = new NuGetCertificate(
                    name: "second",
                    purpose: CommonCertificatePurposes.AzureManagement,
                    environment: "test",
                    target: "Second[def]",
                    x509: null);
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                certs
                    .Setup(c => c.GetCertificatesByPurpose(CommonCertificatePurposes.AzureManagement, StoreName.My, location))
                    .Returns(new[] { firstCert, secondCert });

                // Act
                var azure = new AzureHub(certs.Object);

                // Assert
                Assert.Same(firstCert, azure.ManagementCertificate);
                Assert.Equal("First", azure.SubscriptionName);
                Assert.Equal("abc", azure.SubscriptionId);
            }
        }

        public class TheGetCredentialsMethod
        {
            [Fact]
            public void GivenNoCertificatesInHubAndRequiredFalse_ItReturnsNullCredentials()
            {
                // Arrange
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                var azure = new AzureHub(certs.Object);

                // Act
                var creds = azure.GetCredentials(throwIfMissing: false);

                // Assert
                Assert.Null(creds);
            }

            [Fact]
            public void GivenNoCertificatesInHubAndRequiredTrue_ItThrowsConfigurationException()
            {
                // Arrange
                var certs = new Mock<CertificatesHub>() { CallBase = false };
                var azure = new AzureHub(certs.Object);

                // Act/Assert
                var ex = Assert.Throws<ConfigurationException>(() => azure.GetCredentials(throwIfMissing: true));
                Assert.Equal(Strings.AzureHub_MissingCertificate, ex.Message);
            }
        }
    }
}
