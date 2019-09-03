
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Contracts;
using Microsoft.Azure.Cosmos;
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

        public async Task<IQueryable<T>> GetAsync<T>() where T : IViewModel, new()
        {
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            return client.GetItemLinqQueryable<T>();
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
            var response = await client.Scripts.ExecuteStoredProcedureStreamAsync(storedProcedureName, new PartitionKey(partitionKey) , arguments);
            using (var reader = new StreamReader(response.Content, Encoding.UTF8, false))
            {
                var text = reader.ReadToEnd();
                return text.FromJson<T>();
            }
        }

        public async Task<IEnumerable<TReturnType>> GetAsync<TEntityType, TReturnType>(string query) where TEntityType : IViewModel, new() where TReturnType : new()
        {            
            var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
            var iterator = client.GetItemQueryIterator<TReturnType>(query);
            var results = new List<TReturnType>();
            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                var list = result.ToList();
                if(list.Count > 0)
                    results.AddRange(list);
            }

            return results;
        }


        public IEnumerable<TReturnType> Get<TEntityType, TReturnType>(string query) where TEntityType : IViewModel, new() where TReturnType : new()
        {
            var task = GetAsync<TEntityType, TReturnType>(query);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<T>> GetAsync<T>(string query) where T : IViewModel, new()
        {
            return await GetAsync<T, T>(query).ConfigureAwait(false);
        }

        public IEnumerable<T> Get<T>(string query) where T : IViewModel, new()
        {
            var task = GetAsync<T>(query);
            task.Wait();
            return task.Result;
        }

        public async Task<T> GetAsync<T>(Guid id) where T : IViewModel, new()
        {
            try
            {                
                var client = await ClientFactory.GetClientAsync(Settings).ConfigureAwait(false);
                var response = await client.ReadItemAsync<T>(id.ToString(), Settings.IsPartitioned ? new PartitionKey(id.ToString()) : PartitionKey.None).ConfigureAwait(false);
                return response.Resource;
            }
            catch (CosmosException ex)
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