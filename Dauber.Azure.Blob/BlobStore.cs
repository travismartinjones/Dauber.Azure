using System;
using System.IO;
using System.Threading.Tasks;
using Dauber.Azure.Blob.Contracts;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Dauber.Azure.Blob
{
    public class BlobStore : IBlobStore
    {
        private const string PrivateContainerName = "private";
        private const string PublicContainerName = "public";
        private readonly IBlobSettings _blobSettings;
        
        public BlobStore(IBlobSettings blobSettings)
        {
            if(blobSettings == null)
                throw new ArgumentNullException(nameof(blobSettings), "The blob store must have an implementation of the blob settings. Typically this is placed in your application's container.");

            _blobSettings = blobSettings;            
        }

        public async Task<BlobCreationResponse> InsertAsync(string contentType, string blobName, byte[] data, bool isPrivate = true)
        {
            var container = isPrivate ? await GetPrivateContainer().ConfigureAwait(false) : await GetPublicContainer().ConfigureAwait(false);
            var blockBlob = container.GetBlockBlobReference(blobName);            
            await blockBlob.UploadFromByteArrayAsync(data, 0, data.Length).ConfigureAwait(false);
            blockBlob.Properties.ContentType = contentType;
            await blockBlob.SetPropertiesAsync().ConfigureAwait(false);

            return new BlobCreationResponse
            {
                Url = blockBlob.Uri.AbsoluteUri
            };
        }

        public async Task DeleteAsync(string blobUrl)
        {
            var blockBlob = await GetBlockBlob(blobUrl).ConfigureAwait(false);
            await blockBlob.DeleteAsync().ConfigureAwait(false);
        }

        public async Task<Contracts.Blob> GetAsync(string blobUrl)
        {
            var blockBlob = await GetBlockBlob(blobUrl).ConfigureAwait(false);            

            using (var memoryStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memoryStream).ConfigureAwait(false);
                return new Contracts.Blob
                {
                    ContentType = blockBlob.Properties.ContentType,
                    Data = memoryStream.ToArray()
                };
            }
        }

        private async Task<CloudBlockBlob> GetBlockBlob(string blobUrl)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            var container = await GetContainerForUrl(blobUrl).ConfigureAwait(false);
            return container.GetBlockBlobReference(blobName);
        }

        public async Task<string> GetValetKeyUri(string blobUrl, int secondsToExpireKey)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            var container = await GetContainerForUrl(blobUrl).ConfigureAwait(false);
            var blob = container.GetBlockBlobReference(blobName);

            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(10),
                Permissions = SharedAccessBlobPermissions.Read
            };

            var sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

            return blob.Uri + sasBlobToken;
        }

        private string GetBlobNameFromUrl(string blobUrl)
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blob = new CloudBlockBlob(new Uri(blobUrl), storageAccount.Credentials);
            return blob.Name;
        }

        private async Task<CloudBlobContainer> GetContainerForUrl(string blobUrl)
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blob = new CloudBlockBlob(new Uri(blobUrl), storageAccount.Credentials);
            return blob.Container.Name == PrivateContainerName ? await GetPrivateContainer().ConfigureAwait(false) : await GetPublicContainer().ConfigureAwait(false);
        }

        private async Task<CloudBlobContainer> GetPrivateContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(PrivateContainerName);            
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);            
            return container;
        }

        private async Task<CloudBlobContainer> GetPublicContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(PublicContainerName);

            if (await container.CreateIfNotExistsAsync().ConfigureAwait(false))
            {
                await container.SetPermissionsAsync(new BlobContainerPermissions
                {
                    // set access to public for the container
                    PublicAccess = BlobContainerPublicAccessType.Blob
                }).ConfigureAwait(false);
            }

            return container;
        }
    }
}