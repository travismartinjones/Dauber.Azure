using System;
using System.Collections.Generic;
using System.Linq;
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
        public DocumentDbWritableReadModelRepository(IDocumentDbSettings settings, IReliableReadWriteDocumentClientFactory clientFactory, ILogger logger)
            : base(settings, clientFactory, logger)
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
            var query = $@"SELECT * FROM c WHERE c.id IN ['{string.Join("','",ids)}']";
            await DeleteAsync<T>(query).ConfigureAwait(false);
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

        public void Delete<T>(string query) where T : IViewModel
        {
            DeleteAsync<T>(query).Wait();
        }

        public async Task DeleteAsync<T>(string query) where T : IViewModel
        {
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);

            if(!query.Contains("DocType") && !query.Contains(typeof(T).Name))
               throw new Exception($"The provided query is not filtering to DocType = '{typeof(T).Name}'");

            await client.ExecuteStoredProcedureAsync<T>(UriFactory.CreateStoredProcedureUri(Settings.DocumentDbRepositoryDatabaseId, Settings.DocumentDbRepositoryCollectionId, "bulkDelete"), query).ConfigureAwait(false);            
        } 

        public async Task DeleteAsync<T>(T item) where T : IViewModel
        {            
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            await client.DeleteItemAsync<T>(item.Id.ToString(), new PartitionKey(item.Id.ToString()), new ItemRequestOptions
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
            // add in the entity type to allow multiple document types to share the same collection
            // this is a common cost savings technique with azure document db
            item.DocType = typeof(T).Name;
            var collectionLink = await GetCollectionLinkAsync<T>().ConfigureAwait(false);
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            await InsertAsync(client, collectionLink, item).ConfigureAwait(false);
        }

        public void Insert<T>(IEnumerable<T> items) where T : IViewModel
        {
            InsertAsync(items).Wait();
        }        

        public async Task InsertAsync<T>(IEnumerable<T> items) where T : IViewModel
        {
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            foreach (var item in items)
            {
                await client.UpsertItemAsync(item, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : (PartitionKey?) null);
            }
        }

        protected async Task InsertAsync<T>(Container client, Uri collectionLink, T item) where T : IViewModel
        {
            item.DocType = typeof(T).Name;
            await client.UpsertItemAsync(item, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : (PartitionKey?) null).ConfigureAwait(false);
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

            var documentLink = GetDocumentLink<T>(item.Id.ToString());
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
                        
            await client.ReplaceDocumentAsync(documentLink, item, GetOptimisticConcurrency(item.Id, item.ETag)).ConfigureAwait(false);           
        }

        public async Task DeleteDatabaseAsync()
        {
            Logger.Information(Common.LoggerContext, "Deleting database {0}", Settings.DocumentDbRepositoryDatabaseId);

            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            await client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(Settings.DocumentDbRepositoryDatabaseId)).ConfigureAwait(false);
        }
        
        private RequestOptions GetOptimisticConcurrency(Guid id, string eTag)
        {
            return new RequestOptions
            {
                PartitionKey = Settings.IsPartitioned ? new PartitionKey(id.ToString()) : null,
                AccessCondition = new AccessCondition
                {
                    Condition = eTag,
                    Type = AccessConditionType.IfMatch
                }
            };
        }

        public void Upsert<T>(T item) where T : IViewModel
        {
            UpsertAsync(item).Wait();
        }

        public async Task UpsertAsync<T>(T item) where T : IViewModel
        {
            // add in the entity type to allow multiple document types to share the same collection
            // this is a common cost savings technique with azure document db
            item.DocType = typeof(T).Name;
            var collectionLink = await GetCollectionLinkAsync<T>().ConfigureAwait(false);
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            await UpsertAsync(client, collectionLink, item).ConfigureAwait(false);
        }

        public void Upsert<T>(IEnumerable<T> items) where T : IViewModel
        {
            UpsertAsync(items).Wait();
        }

        public async Task UpsertAsync<T>(IEnumerable<T> items) where T : IViewModel
        {
            var collectionLink = await GetCollectionLinkAsync<T>().ConfigureAwait(false);
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            foreach (var item in items)
            {
                await UpsertAsync(client, collectionLink, item).ConfigureAwait(false);
            }
        }

        protected async Task UpsertAsync<T>(DocumentClient client, Uri collectionLink, T item) where T : IViewModel
        {
            item.DocType = typeof(T).Name;
            await client.UpsertDocumentAsync(collectionLink, item, new RequestOptions { PartitionKey = Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : null}).ConfigureAwait(false);
        }
    }
}