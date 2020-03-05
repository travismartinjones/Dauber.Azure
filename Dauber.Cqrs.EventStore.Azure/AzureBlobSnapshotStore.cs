using System;
using System.Threading.Tasks;
using Dauber.Azure.Blob.Contracts;
using HighIronRanch.Azure.TableStorage;
using HighIronRanch.Cqrs.EventStore.Azure;

namespace Dauber.Cqrs.EventStore.Azure
{
    public class AzureBlobSnapshotStore : IAzureBlobSnapshotStore
    {
        private readonly IBlobStore _blobStore;

        public AzureBlobSnapshotStore(IBlobStore blobStore)
        {
            _blobStore = blobStore;
        }

        public async Task Save(string filename, AzureBlobSnapshot.AzureBlobSnapshotPayload snapshot)
        {
            await _blobStore.InsertAsync("text/json", filename, snapshot.ToBson()).ConfigureAwait(false);
        }

        public async Task<AzureBlobSnapshot.AzureBlobSnapshotPayload> Get(string filename)
        {
            var blob = await _blobStore.GetAsync(filename, true).ConfigureAwait(false);
            return blob.Data.FromBson<AzureBlobSnapshot.AzureBlobSnapshotPayload>();
        }
    }
}