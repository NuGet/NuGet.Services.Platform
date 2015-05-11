// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Configuration;

namespace NuGet.Services
{
    public class CertificatesHub
    {
        private ConfigurationHub _config;

        public CertificatesHub() : this(null)
        {

        }

        public CertificatesHub(ConfigurationHub config)
        {
            _config = config;
        }

        public virtual IEnumerable<NuGetCertificate> GetAllCertificates(StoreName name, StoreLocation location)
        {
            var store = new X509Store(name, location);
            store.Open(OpenFlags.ReadOnly);
            return store
                .Certificates
                .Find(X509FindType.FindByTimeValid, DateTime.Now, validOnly: false)
                .Cast<X509Certificate2>()
                .Select(c => NuGetCertificate.CreateOrDefault(c))
                .Where(c => c != null);
        }

        public virtual IEnumerable<NuGetCertificate> GetCertificatesByPurpose(string purpose, StoreName name, StoreLocation location)
        {
            return GetAllCertificates(name, location)
                .Where(c => String.Equals(c.Purpose, purpose, StringComparison.OrdinalIgnoreCase));
        }

        public virtual NuGetCertificate GetCertificateFromConfigSetting(string setting, StoreName name, StoreLocation location)
        {
            if (_config == null)
            {
                return null;
            }

            string thumbprint = _config.GetSetting(setting);
            if (String.IsNullOrEmpty(thumbprint))
            {
                return null;
            }

            var store = new X509Store(name, location);
            store.Open(OpenFlags.ReadOnly);
            var cert = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
                .Cast<X509Certificate2>()
                .FirstOrDefault();
            return cert == null ? null : NuGetCertificate.Create(cert);
        }
    }
}
