﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NuGet.Services.ServiceModel;
using Xunit;

namespace NuGet.Services.Configuration
{
    public class SqlConfigurationFacts
    {
        [Fact]
        public void LoadsAllKnownSqlServers()
        {
            // Arrange
            var host = new Mock<ServiceHost>();
            host.Setup(h => h.GetConfigurationSetting("Sql.Primary")).Returns("Server=primary");
            host.Setup(h => h.GetConfigurationSetting("Sql.Warehouse")).Returns("Server=warehouse");
            var hub = new ConfigurationHub(host.Object);

            // Act
            var actual = hub.GetSection<SqlConfiguration>();

            // Assert
            Assert.Equal("primary", actual.GetConnectionString(KnownSqlConnection.Primary).DataSource);
            Assert.Equal("warehouse", actual.GetConnectionString(KnownSqlConnection.Warehouse).DataSource);
        }

        [Fact]
        public void IgnoresMissingServers()
        {
            // Arrange
            var host = new Mock<ServiceHost>();
            host.Setup(h => h.GetConfigurationSetting("Sql.Primary")).Returns("Server=primary");
            var hub = new ConfigurationHub(host.Object);

            // Act
            var actual = hub.GetSection<SqlConfiguration>();

            // Assert
            Assert.Equal("primary", actual.GetConnectionString(KnownSqlConnection.Primary).DataSource);
            Assert.Null(actual.GetConnectionString(KnownSqlConnection.Warehouse));
        }

        [Fact]
        public void IgnoresUnknownServers()
        {
            // Arrange
            var host = new Mock<ServiceHost>();
            host.Setup(h => h.GetConfigurationSetting("Sql.Woozle")).Returns("Server=primary");
            var hub = new ConfigurationHub(host.Object);

            // Act
            var actual = hub.GetSection<SqlConfiguration>();

            // Assert
            Assert.Null(actual.GetConnectionString(KnownSqlConnection.Primary));
            Assert.Null(actual.GetConnectionString(KnownSqlConnection.Warehouse));
        }
    }
}
