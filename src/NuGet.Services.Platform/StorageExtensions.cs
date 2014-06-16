using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
