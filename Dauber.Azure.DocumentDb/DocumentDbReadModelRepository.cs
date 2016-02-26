using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Contracts;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Dauber.Azure.DocumentDb
{
    public class DocumentDbReadModelRepository : IViewModelRepository
    {
        protected readonly IDocumentDbSettings Settings;
        protected readonly IReliableReadWriteDocumentClientFactory ClientFactory;
        protected readonly ILogger Logger;

        protected Uri CollectionUri;

        public DocumentDbReadModelRepository(IDocumentDbSettings settings, IReliableReadWriteDocumentClientFactory clientFactory, ILogger logger)
        {
            Settings = settings;
            ClientFactory = clientFactory;
            Logger = logger;
        }

        /// <summary>
        /// Verifies the collection exists and throws an exception if it does not.
        /// Marked virtual so a subclass with writing functionality could create the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected virtual async Task CreateCollectionIfNecessaryAsync<T>()
        {
            var databaseLink = UriFactory.CreateDatabaseUri(Settings.DocumentDbRepositoryDatabaseId);
            var client = await ClientFactory.GetClientAsync(Settings);
            var collection = client.CreateDocumentCollectionQuery(databaseLink)
                                .Where(c => c.Id == Settings.DocumentDbRepositoryCollectionId)
                                .AsEnumerable()
                                .FirstOrDefault();
            if (collection == null)
            {
                Logger.Error(Common.LoggerContext, "Collection {0} does not exist", Settings.DocumentDbRepositoryCollectionId);
                throw new Exception("Collection does not exist.");
            }
        }

        protected async Task<Uri> GetCollectionLinkAsync<T>()
        {
            if (CollectionUri != null) return CollectionUri;

            await CreateCollectionIfNecessaryAsync<T>();

            CollectionUri = UriFactory.CreateDocumentCollectionUri(Settings.DocumentDbRepositoryDatabaseId, Settings.DocumentDbRepositoryCollectionId);
            return CollectionUri;
        }

        protected Uri GetDocumentLink<T>(string documentId)
        {
            return UriFactory.CreateDocumentUri(Settings.DocumentDbRepositoryDatabaseId, Settings.DocumentDbRepositoryCollectionId, documentId);
        }

        public async Task<IQueryable<T>> GetAsync<T>() where T : IViewModel, new()
        {
            var collectionLink = await GetCollectionLinkAsync<T>();
            var client = await ClientFactory.GetClientAsync(Settings);
            var queryable = client.CreateDocumentQuery<T>(collectionLink);

            // filter the entity by type due to multiple document types sharing the same collection
            return queryable.Where(x => x.DocType == typeof(T).Name);
        }

        public IQueryable<T> Get<T>() where T : IViewModel, new()
        {
            var task = GetAsync<T>();
            task.Wait();
            return task.Result;
        }

        public async Task<T> GetAsync<T>(Guid id) where T : IViewModel, new()
        {
            var documentLink = GetDocumentLink<T>(id.ToString());
            var client = await ClientFactory.GetClientAsync(Settings);
            var response = await client.ReadDocumentAsync(documentLink);
            return JsonConvert.DeserializeObject<T>(response.Resource.ToString());
        }

        public T Get<T>(Guid id) where T : IViewModel, new()
        {
            var task = GetAsync<T>(id);
            task.Wait();
            return task.Result;
        }
    }
}