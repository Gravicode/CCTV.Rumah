using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types
using System.IO;
using System.Diagnostics;

namespace CCTV_Accord
{
    public class AzureBlobHelper
    {
        public CloudBlobContainer container { get; set; }
        public AzureBlobHelper()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                APPCONTANTS.BlobConnString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            container = blobClient.GetContainerReference(APPCONTANTS.BlobContainerName);

            // Create the container if it doesn't already exist.
            Task<bool> a = container.CreateIfNotExistsAsync();

            a.Wait();
        }

        public async Task<bool> UploadFile(MemoryStream ms, string blobName)
        {
            try
            {
                // Retrieve reference to a blob named "myblob".
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                var bytes = ms.ToArray();
                // Create or overwrite the "myblob" blob with contents from a local file.
                await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);


                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace + "_" + ex.Message);
                return false;
            }
        }
    }
}