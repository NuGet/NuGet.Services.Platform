// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using NuGet.Services.Configuration;

namespace NuGet.Services.Work.Azure
{
    public class AzureHub
    {
        public static readonly string ConfigSetting = "Azure.ManagementCertificateThumbprint";
        private static readonly Regex TargetNameMatcher = new Regex(@"(?<name>[^,]+)\[(?<id>[^\]]+)\]");
        
        public NuGetCertificate ManagementCertificate { get; private set; }
        public string SubscriptionId { get; private set; }
        public string SubscriptionName { get; private set; }

        public AzureHub(CertificatesHub certs)
        {
            ManagementCertificate = FindCert(certs);
            if (ManagementCertificate != null)
            {
                LoadSubscriptionIdentity();
            }
        }

        public SubscriptionCloudCredentials GetCredentials(bool throwIfMissing)
        {
            if (ManagementCertificate == null)
            {
                if (throwIfMissing)
                {
                    throw new ConfigurationException(Strings.AzureHub_MissingCertificate);
                }
                else
                {
                    return null;
                }
            }

            AzureHubEventSource.Log.UsingCredentials(SubscriptionName, SubscriptionId, ManagementCertificate.Thumbprint);
            return new CertificateCloudCredentials(SubscriptionId, ManagementCertificate.Certificate);
        }

        private void LoadSubscriptionIdentity()
        {
            // Read it from the cert
            if (String.IsNullOrEmpty(ManagementCertificate.Target))
            {
                throw new ConfigurationException(Strings.AzureHub_MissingSubscription);
            }
            var match = TargetNameMatcher.Match(ManagementCertificate.Target);
            if (!match.Success)
            {
                throw new ConfigurationException(Strings.AzureHub_MissingSubscription);
            }
            SubscriptionId = match.Groups["id"].Value;
            SubscriptionName = match.Groups["name"].Value;
            
            Debug.Assert(!String.IsNullOrEmpty(SubscriptionId) && !String.IsNullOrEmpty(SubscriptionName));
        }

        private NuGetCertificate FindCert(CertificatesHub certs)
        {
            return
                FindCert(certs, StoreLocation.LocalMachine) ??
                FindCert(certs, StoreLocation.CurrentUser);
        }

        private NuGetCertificate FindCert(CertificatesHub certs, StoreLocation storeLocation)
        {
            var specificMatch = certs.GetCertificateFromConfigSetting(ConfigSetting, StoreName.My, storeLocation);
            if(specificMatch != null) 
            {
                AzureHubEventSource.Log.SingleMatch(storeLocation.ToString(), specificMatch.Thumbprint, specificMatch.Subject);
                return specificMatch;
            }

            var candidates = certs
                .GetCertificatesByPurpose(CommonCertificatePurposes.AzureManagement, StoreName.My, storeLocation)
                .ToList();
            // No candidates? Return null.
            if (candidates.Count == 0)
            {
                AzureHubEventSource.Log.NoMatch(storeLocation.ToString());
                return null;
            }
            // One candidate? Return it.
            else if (candidates.Count == 1)
            {
                AzureHubEventSource.Log.SingleMatch(storeLocation.ToString(), candidates[0].Thumbprint, candidates[0].Subject);
                return candidates[0];
            }
            // Multiple candidates? Return the first one
            else
            {
                var match = candidates.FirstOrDefault();
                AzureHubEventSource.Log.MultipleMatches(storeLocation.ToString(), match.Thumbprint, match.Subject);
                return match;
            }
        }
    }
}
