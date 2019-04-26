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

        private CloudBlobContainer PublicContainer => GetPublicContainer();
        private CloudBlobContainer PrivateContainer => GetPrivateContainer();

        public BlobStore(IBlobSettings blobSettings)
        {
            if(blobSettings == null)
                throw new ArgumentNullException(nameof(blobSettings), "The blob store must have an implementation of the blob settings. Typically this is placed in your application's container.");

            _blobSettings = blobSettings;            
        }

        public async Task<BlobCreationResponse> InsertAsync(string contentType, string blobName, byte[] data, bool isPrivate = true)
        {
            var container = isPrivate ? PrivateContainer : PublicContainer;
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
            var blockBlob = GetBlockBlob(blobUrl);
            await blockBlob.DeleteAsync().ConfigureAwait(false);
        }

        public async Task<Contracts.Blob> GetAsync(string blobUrl)
        {
            var blockBlob = GetBlockBlob(blobUrl);            

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

        private CloudBlockBlob GetBlockBlob(string blobUrl)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            var container = GetContainerForUrl(blobUrl);
            return container.GetBlockBlobReference(blobName);
        }

        public string GetValetKeyUri(string blobUrl, int secondsToExpireKey)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            var container = GetContainerForUrl(blobUrl);
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

        private CloudBlobContainer GetContainerForUrl(string blobUrl)
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blob = new CloudBlockBlob(new Uri(blobUrl), storageAccount.Credentials);
            return blob.Container.Name == PrivateContainerName ? PrivateContainer : PublicContainer;
        }

        private CloudBlobContainer GetPrivateContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(PrivateContainerName);
            container.CreateIfNotExists();            
            return container;
        }

        private CloudBlobContainer GetPublicContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(PublicContainerName);

            if (container.CreateIfNotExists())
            {
                container.SetPermissions(new BlobContainerPermissions
                {
                    // set access to public for the container
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            }

            return container;
        }
    }
}