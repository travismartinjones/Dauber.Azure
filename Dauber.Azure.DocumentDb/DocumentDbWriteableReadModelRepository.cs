using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Contracts;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Client.TransientFaultHandling;

namespace Dauber.Azure.DocumentDb
{
    public class DocumentDbWritableReadModelRepository : DocumentDbReadModelRepository, IWritableViewModelRepository
    {
        public DocumentDbWritableReadModelRepository(IDocumentDbSettings settings, IReliableReadWriteDocumentClientFactory clientFactory, ILogger logger)
            : base(settings, clientFactory, logger)
        {

        }

        /// <summary>
        /// Verifies the collection exists and creates it if it does not.
        /// Overrides base class implementation in order to add creation logic.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected override async Task CreateCollectionIfNecessaryAsync<T>()
        {
            var databaseLink = UriFactory.CreateDatabaseUri(Settings.DocumentDbRepositoryDatabaseId);
            var client = await ClientFactory.GetClientAsync(Settings);
            var collection = client.CreateDocumentCollectionQuery(databaseLink)
                                .Where(c => c.Id == Settings.DocumentDbRepositoryCollectionId)
                                .AsEnumerable()
                                .FirstOrDefault();
            if (collection == null)
            {
                Logger.Information(Common.LoggerContext, "Creating collection {0}", typeof(T).Name);
                await client.CreateDocumentCollectionAsync(databaseLink, new DocumentCollection() { Id = typeof(T).Name });
            }
        }

        public void Delete<T>(T item) where T : IViewModel
        {
            DeleteAsync(item).Wait();
        }
        
        public async Task DeleteAsync<T>(T item) where T : IViewModel
        {
            var documentLink = GetDocumentLink<T>(item.Id.ToString());
            var client = await ClientFactory.GetClientAsync(Settings);
            await client.DeleteDocumentAsync(documentLink);
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
            var collectionLink = await GetCollectionLinkAsync<T>();
            var client = await ClientFactory.GetClientAsync(Settings);
            await InsertAsync(client, collectionLink, item);
        }

        public void Insert<T>(IEnumerable<T> items) where T : IViewModel
        {
            InsertAsync(items).Wait();
        }

        public async Task UpdateAsync<T>(T item) where T : IViewModel
        {
            var collectionLink = await GetCollectionLinkAsync<T>();
            var client = await ClientFactory.GetClientAsync(Settings);
            await client.UpsertDocumentAsync(collectionLink, item);
        }
        
        public void Update<T>(T item) where T : IViewModel
        {
            UpdateAsync(item).Wait();
        }

        public async Task InsertAsync<T>(IEnumerable<T> items) where T : IViewModel
        {
            var collectionLink = await GetCollectionLinkAsync<T>();
            var client = await ClientFactory.GetClientAsync(Settings);
            foreach (var item in items)
            {
                await InsertAsync(client, collectionLink, item);
            }
        }

        protected async Task InsertAsync<T>(IReliableReadWriteDocumentClient client, Uri collectionLink, T item) where T : IViewModel
        {
            item.DocType = typeof(T).Name;
            await client.CreateDocumentAsync(collectionLink, item);
        }

        public void Save<T>(T item) where T : IViewModel
        {
            SaveAsync(item).Wait();
        }

        public async Task SaveAsync<T>(T item) where T : IViewModel
        {
            var documentLink = GetDocumentLink<T>(item.Id.ToString());
            var client = await ClientFactory.GetClientAsync(Settings);
            await client.ReplaceDocumentAsync(documentLink, item);
        }

        public void Truncate<T>() where T : IViewModel
        {
            throw new NotImplementedException();
            //TruncateAsync<T>().Wait();
        }

        public async Task TruncateAsync<T>() where T : IViewModel
        {
            throw new NotImplementedException();
            //Logger.Information(Common.LoggerContext, "Deleting all instance of type {0}", typeof(T).Name);

            //var collectionLink = await GetCollectionLinkAsync<T>();
            //var client = await ClientFactory.GetClientAsync(Settings);
            //await client.DeleteDocumentCollectionAsync(collectionLink);
        }

        public async Task DeleteDatabaseAsync()
        {
            Logger.Information(Common.LoggerContext, "Deleting database {0}", Settings.DocumentDbRepositoryDatabaseId);

            var client = await ClientFactory.GetClientAsync(Settings);
            await client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(Settings.DocumentDbRepositoryDatabaseId));
        }
    }
}