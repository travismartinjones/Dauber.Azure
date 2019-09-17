using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using static System.String;

namespace Dauber.Azure.DocumentDb
{
    public class DocumentDbWritableReadModelRepository : DocumentDbReadModelRepository, IWritableViewModelRepository
    {
        public DocumentDbWritableReadModelRepository(IDocumentDbSettings settings, IReliableReadWriteDocumentClientFactory containerFactory, ILogger logger)
            : base(settings, containerFactory, logger)
        {

        }

        public void Delete<T>(T item) where T : IViewModel
        {
            DeleteAsync(item).Wait();
        }

        public void Delete<T>(IEnumerable<T> items) where T : IViewModel
        {
            DeleteAsync<T>(items).Wait();
        }

        public async Task DeleteAsync<T>(params Guid[] ids) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var tasks = ids.Select(id => DeleteByIdAsync<T>(container, id));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task DeleteByIdAsync<T>(Container container, Guid id) where T : IViewModel
        {
            var key = id.ToString();
            try
            {
                await container.DeleteItemAsync<T>(key, Settings.IsPartitioned ? new PartitionKey(key) : PartitionKey.None).ConfigureAwait(false);
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound) return;
                throw;
            }
        }

        public void Delete<T>(params Guid[] ids) where T : IViewModel
        {
            DeleteAsync<T>(ids).Wait();
        }

        public async Task DeleteAsync<T>(IEnumerable<T> items) where T : IViewModel
        {
            if (items == null) return;
            var evaluatedItems = items as T[] ?? items.ToArray();
            if(!evaluatedItems.Any()) return;
            await DeleteAsync<T>(evaluatedItems.Select(x => x.Id).ToArray()).ConfigureAwait(false);
        }        

        public async Task DeleteAsync<T>(T item) where T : IViewModel
        {            
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            await container.DeleteItemAsync<T>(item.Id.ToString(), Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None, new ItemRequestOptions
            {
                IfMatchEtag = item.ETag
            }).ConfigureAwait(false);
        }

        public void Insert<T>(T item) where T : IViewModel
        {
            InsertAsync(item).Wait();
        }

        public async Task InsertAsync<T>(T item) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            await InsertAsync(container, item).ConfigureAwait(false);
        }

        public void Insert<T>(IEnumerable<T> items) where T : IViewModel
        {
            InsertAsync(items).Wait();
        }        

        public async Task InsertAsync<T>(IEnumerable<T> items) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            foreach (var item in items)
            {
                await InsertAsync(container, item).ConfigureAwait(false);
            }
        }

        protected async Task InsertAsync<T>(Container container, T item) where T : IViewModel
        {
            // add in the entity type to allow multiple document types to share the same collection
            // this is a common cost savings technique with azure document db
            item.DocType = typeof(T).Name;
            await container.CreateItemAsync(item, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None).ConfigureAwait(false);
        }

        public void Update<T>(T item) where T : IViewModel
        {
            UpdateAsync(item).Wait();
        }

        public async Task UpdateAsync<T>(T item) where T : IViewModel
        {
            if (IsNullOrEmpty(item.ETag))
            {
                throw new OptimisticConcurrencyEtagMissingException($"An attempt to update {item.DocType} {item.ETag} without an ETag. If using a custom query, ensure the _etag property is included in your result set.");
            }
            
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var id = item.Id.ToString();
            await container.ReplaceItemAsync(item, id, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None, new ItemRequestOptions
            {
                IfMatchEtag = item.ETag
            }).ConfigureAwait(false);
        }                

        public void Upsert<T>(T item) where T : IViewModel
        {
            UpsertAsync(item).Wait();
        }

        public async Task UpsertAsync<T>(T item) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            await UpsertAsync(container, item).ConfigureAwait(false);
        }

        public void Upsert<T>(IEnumerable<T> items) where T : IViewModel
        {
            UpsertAsync(items).Wait();
        }

        public async Task UpsertAsync<T>(IEnumerable<T> items) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            foreach (var item in items)
            {
                await UpsertAsync(container, item).ConfigureAwait(false);
            }
        }

        protected async Task UpsertAsync<T>(Container container, T item) where T : IViewModel
        {
            item.DocType = typeof(T).Name;
            await container.UpsertItemAsync(item, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None, new ItemRequestOptions
            {
                IfMatchEtag = item.ETag
            }).ConfigureAwait(false);
        }
    }
}