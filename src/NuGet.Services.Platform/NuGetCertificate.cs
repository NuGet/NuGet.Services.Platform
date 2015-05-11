// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    public class NuGetCertificate
    {
        public static readonly string NuGetCertificateMarker = "nuget-services";

        public string Name { get; private set; }
        public IReadOnlyList<string> Subtargets { get; private set; }
        public string Target { get; private set; }
        public string Environment { get; private set; }
        public string Purpose { get; private set; }
        public X509Certificate2 Certificate { get; private set; }

        public string Thumbprint { get { return Certificate == null ? null : Certificate.Thumbprint; } }
        public string Subject { get { return Certificate == null ? null : Certificate.Subject; } }

        public NuGetCertificate(X509Certificate2 x509)
            : this(null, null, null, null, Enumerable.Empty<string>(), x509) { }

        public NuGetCertificate(string name, string purpose, string environment, string target, X509Certificate2 x509)
            : this(name, purpose, environment, target, Enumerable.Empty<string>(), x509) { }
        
        public NuGetCertificate(string name, string purpose, string environment, string target, IEnumerable<string> subtargets, X509Certificate2 x509)
        {
            Name = name;
            Subtargets = subtargets.ToList().AsReadOnly();
            Target = target;
            Environment = environment;
            Purpose = purpose;
            Certificate = x509;
        }

        public static NuGetCertificate Create(X509Certificate2 x509)
        {
            return
                CreateOrDefault(x509) ??
                new NuGetCertificate(null, null, null, null, Enumerable.Empty<string>(), x509);
        }

        public static NuGetCertificate CreateOrDefault(X509Certificate2 x509)
        {
            return CreateOrDefault(x509, x509.Subject);
        }

        internal static NuGetCertificate CreateOrDefault(X509Certificate2 x509, string subjectName)
        {
            // Sample Name:
            //  CN = <name>
            //  OU = <target> /* OPTIONAL */
            //  OU = <purpose>
            //  OU = <environment>

            // Parse the subject name fragments
            var fragments = subjectName.Split(',').Select(s =>
            {
                var splat = s.Split('=');
                if (splat.Length != 2)
                {
                    return null;
                }
                return Tuple.Create(splat[0].Trim(), splat[1].Trim());
            }).Where(t => t != null);

            // Read the fragments in order
            Tuple<string, string> fragment;

            // 1. Name. Must be a CN field
            string name;
            fragments = TakeFragment(fragments, out fragment);
            if (fragment == null || !String.Equals(fragment.Item1, "CN", StringComparison.OrdinalIgnoreCase))
            {
                return null; // Unknown cert!
            }
            else
            {
                name = fragment.Item2;
            }

            // 2. Grab all the OU fields in order and reverse them
            var ouFields = fragments
                .TakeWhile(t => String.Equals(t.Item1, "OU", StringComparison.OrdinalIgnoreCase))
                .Reverse()
                .Select(t => t.Item2); // We know these are all OU fields.

            // The first one must be 'nuget-services' as a marker
            string field;
            ouFields = TakeFragment(ouFields, out field);
            if(!String.Equals(field, NuGetCertificateMarker, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Collect the fields
            string purpose = null;
            string environment = null;
            string target = null;
            IEnumerable<string> subtargets = null;

            ouFields = TakeFragment(ouFields, out field);
            if (!String.IsNullOrEmpty(field))
            {
                environment = field;
            }

            ouFields = TakeFragment(ouFields, out field);
            if (!String.IsNullOrEmpty(field))
            {
                purpose = field;
            }

            ouFields = TakeFragment(ouFields, out field);
            if (!String.IsNullOrEmpty(field))
            {
                target = field;
            }

            // Remaining fields are subtargets
            subtargets = ouFields;

            // Create the certificate!
            return new NuGetCertificate(name, purpose, environment, target, subtargets, x509);
        }

        private static IEnumerable<T> TakeFragment<T>(IEnumerable<T> fragments, out T fragment)
        {
            fragment = fragments.FirstOrDefault();
            return fragments.Skip(1);
        }
    }

    public static class CommonCertificatePurposes
    {
        public static readonly string AzureManagement = "azure-management";
        public static readonly string CertificateAuthority = "ca";
        public static readonly string ClientAuthentication = "client-auth";
    }
}
