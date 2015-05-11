// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NuGet.Services
{
    public class AssemblyInformation
    {
        [JsonConverter(typeof(AssemblyFullNameConverter))]
        public AssemblyName FullName { get; private set; }
        public string BuildBranch { get; private set; }
        public string BuildCommit { get; private set; }
        public DateTimeOffset BuildDate { get; private set; }
        public Uri SourceCodeRepository { get; private set; }
        public string SemanticVersion { get; private set; }

        [JsonConstructor]
        public AssemblyInformation(string fullName, string buildBranch, string buildCommit, string buildDate, string sourceCodeRepository, string semanticVersion)
            : this(new AssemblyName(fullName), buildBranch, buildCommit, buildDate, sourceCodeRepository, semanticVersion)
        {
        }

        public AssemblyInformation(AssemblyName fullName, string buildBranch, string buildCommit, string buildDate, string sourceCodeRepository, string semanticVersion)
        {
            FullName = fullName;
            BuildBranch = buildBranch;
            BuildCommit = buildCommit;
            SemanticVersion = semanticVersion;

            DateTimeOffset date;
            if (DateTimeOffset.TryParse(buildDate, out date))
            {
                BuildDate = date;
            }

            Uri repo;
            if (Uri.TryCreate(sourceCodeRepository, UriKind.RelativeOrAbsolute, out repo))
            {
                SourceCodeRepository = repo;
            }
        }

        public AssemblyInformation(AssemblyName fullName, string buildBranch, string buildCommit, DateTimeOffset buildDate, Uri sourceCodeRepository, string semanticVersion)
            : this(buildBranch, buildCommit, buildDate, sourceCodeRepository, semanticVersion)
        {
            FullName = fullName;
        }

        public AssemblyInformation(string buildBranch, string buildCommit, DateTimeOffset buildDate, Uri sourceCodeRepository, string semanticVersion)
        {
            BuildBranch = buildBranch;
            BuildCommit = buildCommit;
            BuildDate = buildDate;
            SourceCodeRepository = sourceCodeRepository;
            SemanticVersion = semanticVersion;
        }
    }
}
