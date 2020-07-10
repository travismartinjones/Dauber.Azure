using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dauber.Azure.Blob;
using Dauber.Azure.Blob.Contracts;
using Dauber.Core.Container;
using Dauber.Core.Contracts;
using HighIronRanch.Azure.TableStorage;
using HighIronRanch.Cqrs.EventStore.Azure;
using NUnit.Framework;
using Shouldly;
using SimpleCqrs.Domain;

namespace Dauber.Cqrs.EventStore.Azure.Test
{
    public class BlobSettings : IBlobSettings
    {
        public string ConnectionString => "DefaultEndpointsProtocol=https;AccountName=dauberfleetblobdev;AccountKey=vSikpaLJIaEUNRJXJpbeREUyXxbgaJy2iZHLjT15NoZu9I4Ug1A52dpsCj2nGG79h/JHNCVVZxewILYxCxckHg==;EndpointSuffix=core.windows.net";
        public bool IsContainerCreatedIfMissing => false;
    }

    public class AzureTableSettings : IAzureTableSettings
    {
        public string AzureStorageConnectionString => "DefaultEndpointsProtocol=https;AccountName=dauberfleetdeves;AccountKey=c8LQs9btJsfNkdGHRLwZHkynP6MyPsHwtZkkQ7Xh/2kj0YHDeaurDVKwPRcD5NfxhW9XFG1rEUXoEXPMarsbOw==";
    }

    public class SnapshotContent : Snapshot
    {
        public List<string> Content { get; set; } = new List<string>();
        
        public void SetToLength(int length)
        {
            for(var i=0; i < length; i++)
                Content.Add(Guid.NewGuid().ToString());
        }
    }

    public class AzureBlobSnapshotStoreSpecs
    {
        [Test]
        public async Task when_the_snapshot_is_under_table_storage_limits_it_should_save_and_load_to_table_storage()
        {
            var blobStore = new BlobStore(new BlobSettings());
            var azureBlobStore = new AzureBlobSnapshotStore(blobStore);
            var azureTableService = new AzureTableService(new AzureTableSettings());
            var sut = new AzureTableSnapshotStore(
                azureBlobStore,
                new AzureSnapshotBuilder(azureBlobStore),
                azureTableService,
                new DomainEntityTypeBuilder()
            );
            var item = new SnapshotContent {AggregateRootId = new Guid("b87bc654-3d76-4a46-ab58-c71c16b6a000")};
            item.SetToLength(10);
            await sut.SaveSnapshot(item).ConfigureAwait(false);
            var snapshot = await sut.GetSnapshot(item.AggregateRootId).ConfigureAwait(false);
            snapshot.ShouldNotBeNull();
            var cast = snapshot as SnapshotContent;
            cast.ShouldNotBeNull();
            cast.Content.Count.ShouldBe(item.Content.Count);
        }

        [Test]
        public async Task when_the_snapshot_is_over_table_storage_limits_it_should_save_and_load_to_blob_storage()
        {
            var blobStore = new BlobStore(new BlobSettings());
            var azureBlobStore = new AzureBlobSnapshotStore(blobStore);
            var azureTableService = new AzureTableService(new AzureTableSettings());
            var domainEntityTypeBuilder = new DomainEntityTypeBuilder();
            var sut = new AzureTableSnapshotStore(
                azureBlobStore,
                new AzureSnapshotBuilder(azureBlobStore),
                azureTableService,
                domainEntityTypeBuilder
            );
            var item = new SnapshotContent {AggregateRootId = new Guid("cc3ea6b8-6396-4c95-be2e-4d90f2c23736")};
            
            // set a new AggregateRootId for this to be tested
            //var snapshot = await sut.GetSnapshot(item.AggregateRootId).ConfigureAwait(false);
            //snapshot.ShouldBeNull(); 

            item.SetToLength(99999);
            await sut.SaveSnapshot(item).ConfigureAwait(false);
            var snapshot = await sut.GetSnapshot(item.AggregateRootId).ConfigureAwait(false);
            snapshot.ShouldNotBeNull();
            var cast = snapshot as SnapshotContent ;
            cast.ShouldNotBeNull();
            cast.Content.Count.ShouldBe(item.Content.Count);
        }

        [Test]
        public async Task when_the_snapshot_changes_from_under_to_over_limits()
        {
            var blobStore = new BlobStore(new BlobSettings());
            var azureBlobStore = new AzureBlobSnapshotStore(blobStore);
            var azureTableService = new AzureTableService(new AzureTableSettings());
            var sut = new AzureTableSnapshotStore(
                azureBlobStore,
                new AzureSnapshotBuilder(azureBlobStore),
                azureTableService,
                new DomainEntityTypeBuilder()
            );
            var item = new SnapshotContent {AggregateRootId = new Guid("e8ae8dd3-d3fd-434a-9775-82887bf1b2ce")};
            item.SetToLength(10);
            await sut.SaveSnapshot(item).ConfigureAwait(false);
            var snapshot = await sut.GetSnapshot(item.AggregateRootId).ConfigureAwait(false);
            snapshot.ShouldNotBeNull();
            var cast = snapshot as SnapshotContent;
            cast.ShouldNotBeNull();
            cast.Content.Count.ShouldBe(item.Content.Count);

            item.Content.Clear();
            item.SetToLength(99999);

            await sut.SaveSnapshot(item).ConfigureAwait(false);
            snapshot = await sut.GetSnapshot(item.AggregateRootId).ConfigureAwait(false);
            snapshot.ShouldNotBeNull();
            cast = snapshot as SnapshotContent ;
            cast.ShouldNotBeNull();
            cast.Content.Count.ShouldBe(item.Content.Count);
        }
    }
}