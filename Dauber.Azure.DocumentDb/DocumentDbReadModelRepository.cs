
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core.Contracts;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Dauber.Azure.DocumentDb
{
    public class DocumentDbReadModelRepository : IViewModelRepository
    {
        protected readonly IDocumentDbSettings Settings;
        protected readonly IReliableReadWriteDocumentClientFactory ClientFactory;
        protected readonly Core.ILogger Logger;

        protected Uri CollectionUri;

        public DocumentDbReadModelRepository(IDocumentDbSettings settings, IReliableReadWriteDocumentClientFactory clientFactory, Core.ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
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
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
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

            if(Settings.IsCollectionCretedIfMissing)
                await CreateCollectionIfNecessaryAsync<T>().ConfigureAwait(false);

            CollectionUri = UriFactory.CreateDocumentCollectionUri(Settings.DocumentDbRepositoryDatabaseId, Settings.DocumentDbRepositoryCollectionId);
            return CollectionUri;
        }

        protected Uri GetDocumentLink<T>(string documentId)
        {
            return UriFactory.CreateDocumentUri(Settings.DocumentDbRepositoryDatabaseId, Settings.DocumentDbRepositoryCollectionId, documentId);
        }

        public async Task<IQueryable<T>> GetAsync<T>() where T : IViewModel, new()
        {
            var collectionLink = await GetCollectionLinkAsync<T>().ConfigureAwait(false);
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            var queryable = client.CreateDocumentQuery<T>(collectionLink, new FeedOptions { EnableCrossPartitionQuery = true, MaxDegreeOfParallelism = 10, MaxBufferedItemCount = 100});

            // filter the entity by type due to multiple document types sharing the same collection
            return queryable.Where(x => x.DocType == typeof(T).Name);
        }

        public IQueryable<T> Get<T>() where T : IViewModel, new()
        {
            var task = GetAsync<T>();
            task.Wait();
            return task.Result;
        }

        public T GetByStoredProcedure<T>(string storedProcedureName, params dynamic[] arguments) where T : IViewModel, new()
        {
            return GetByStoredProcedureAsync<T>(storedProcedureName, arguments).Result;
        }

        public async Task<T> GetByStoredProcedureAsync<T>(string storedProcedureName, params dynamic[] arguments) where T : IViewModel, new()
        {
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            return await client.ExecuteStoredProcedureAsync<T>(UriFactory.CreateStoredProcedureUri(Settings.DocumentDbRepositoryDatabaseId, Settings.DocumentDbRepositoryCollectionId, storedProcedureName), arguments).ConfigureAwait(false);
        }

        public async Task<IQueryable<TReturnType>> GetAsync<TEntityType, TReturnType>(string query) where TEntityType : IViewModel, new() where TReturnType : new()
        {
            var collectionLink = await GetCollectionLinkAsync<TEntityType>().ConfigureAwait(false);
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            return client.CreateDocumentQuery<TReturnType>(collectionLink, query);
        }


        public IQueryable<TReturnType> Get<TEntityType, TReturnType>(string query) where TEntityType : IViewModel, new() where TReturnType : new()
        {
            var task = GetAsync<TEntityType, TReturnType>(query);
            task.Wait();
            return task.Result;
        }

        public async Task<IQueryable<T>> GetAsync<T>(string query) where T : IViewModel, new()
        {
            return await GetAsync<T, T>(query).ConfigureAwait(false);
        }

        public IQueryable<T> Get<T>(string query) where T : IViewModel, new()
        {
            var task = GetAsync<T>(query);
            task.Wait();
            return task.Result;
        }

        public async Task<T> GetAsync<T>(Guid id) where T : IViewModel, new()
        {
            try
            {
                var documentLink = GetDocumentLink<T>(id.ToString());
                var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
                var response = await client.ReadDocumentAsync(documentLink).ConfigureAwait(false);
                var viewModel = JsonConvert.DeserializeObject<T>(response.Resource.ToString());                
                return viewModel;
            }
            catch (DocumentClientException ex)
            {
                // don't throw an exception if the resource is not found, instead, indicate this by returning null
                if(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return default(T);

                throw;
            }
        }

        public T Get<T>(Guid id) where T : IViewModel, new()
        {
            var task = GetAsync<T>(id);
            task.Wait();
            return task.Result;
        }
    }
}