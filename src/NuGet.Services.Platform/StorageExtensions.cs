// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using NuGet.Services.Client;

namespace Microsoft.WindowsAzure.Storage
{
    public static class StorageExtensions
    {
        public static string GetConnectionString(this CloudStorageAccount self)
        {
            return
                "AccountName=" + self.Credentials.AccountName + ";" +
                "AccountKey=" + self.Credentials.ExportBase64EncodedKey() + ";" +
                "DefaultEndpointsProtocol=https";
        }
    }
}

namespace Microsoft.WindowsAzure.Storage.Blob
{
    public static class BlobStorageExtensions
    {
        public static async Task<CloudBlockBlob> UploadBlob(this CloudBlobContainer self, string path, string sourceFileName, string contentType)
        {
            CloudBlockBlob blob = null;
            try
            {
                blob = self.GetBlockBlobReference(path);
                blob.Properties.ContentType = contentType;
                await blob.UploadFromFileAsync(sourceFileName, FileMode.Open);
            }
            catch (StorageException stex)
            {
                if (stex.RequestInformation != null &&
                    stex.RequestInformation.ExtendedErrorInformation != null &&
                    (stex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound ||
                     stex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound))
                {
                    // Ignore the error
                }
                else
                {
                    throw;
                }
            }
            return blob;
        }

        public static async Task<CloudBlockBlob> UploadJsonBlob(this CloudBlobContainer self, string path, object content)
        {
            CloudBlockBlob blob = null;
            try
            {
                blob = self.GetBlockBlobReference(path);
                blob.Properties.ContentType = "application/json";
                await blob.UploadTextAsync(JsonFormat.Serialize(content));
            }
            catch (StorageException stex)
            {
                if (stex.RequestInformation != null &&
                    stex.RequestInformation.ExtendedErrorInformation != null &&
                    (stex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound ||
                     stex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound))
                {
                    // Ignore the error
                }
                else
                {
                    throw;
                }
            }
            return blob;
        }

        public static async Task<CloudBlockBlob> DownloadBlob(this CloudBlobContainer self, string path, string destinationFileName)
        {
            CloudBlockBlob blob = null;
            try
            {
                blob = self.GetBlockBlobReference(path);
                await blob.DownloadToFileAsync(destinationFileName, FileMode.Create);
            }
            catch (StorageException stex)
            {
                if (stex.RequestInformation != null &&
                    stex.RequestInformation.ExtendedErrorInformation != null &&
                    (stex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound ||
                     stex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound))
                {
                    // Ignore the error
                }
                else
                {
                    throw;
                }
            }
            return blob;
        }
    }
}
