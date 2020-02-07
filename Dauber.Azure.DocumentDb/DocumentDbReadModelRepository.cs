using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Dauber.Core.Contracts;
using Microsoft.Azure.Cosmos;

namespace Dauber.Azure.DocumentDb
{
    public class DocumentDbReadModelRepository : IViewModelRepository
    {
        protected readonly IDocumentDbSettings Settings;
        protected readonly IReliableReadWriteDocumentClientFactory ClientFactory;
        protected readonly Core.ILogger Logger;
        private readonly ITelemetryLogger _telemetryLogger;

        protected Uri CollectionUri;

        public DocumentDbReadModelRepository(
            IDocumentDbSettings settings, 
            IReliableReadWriteDocumentClientFactory clientFactory, 
            Core.ILogger logger, 
            ITelemetryLogger telemetryLogger)
        {
            Settings = settings;
            ClientFactory = clientFactory;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryLogger = telemetryLogger;
        }

        protected string GetFilename(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public async Task<IQueryable<T>> GetAsync<T>() where T : IViewModel, new()
        {
            var client = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            return client.GetItemLinqQueryable<T>(true, null, new QueryRequestOptions
            {
                
            }).Where(x => x.DocType == typeof(T).Name);
        }

        public IQueryable<T> Get<T>() where T : IViewModel, new()
        {
            var task = GetAsync<T>();
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<TReturnType>> GetAsync<TEntityType, TReturnType>(string query, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where TEntityType : IViewModel, new() where TReturnType : new()
        {            
            var client = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var iterator = client.GetItemQueryIterator<TReturnType>(query, null, new QueryRequestOptions
            {
                MaxConcurrency = 1
            });
            var results = new List<TReturnType>();
            double requestCharge = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (iterator.HasMoreResults)
            {
                try
                {
                    var result = await iterator.ReadNextAsync().ConfigureAwait(false);
                    requestCharge += result.RequestCharge;
                    var list = result.ToList();
                    if (list.Count > 0)
                        results.AddRange(list);
                }                
                catch (CosmosException ex)
                {
                    // don't throw an exception if the resource is not found, instead, indicate this by returning null
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) continue;

                    throw;
                }
            }
            stopwatch.Stop();
            
            if(_telemetryLogger != null)
                await _telemetryLogger.Log("GetAsync", requestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, query).ConfigureAwait(false);

            return results;
        }
        
        public IEnumerable<TReturnType> Get<TEntityType, TReturnType>(string query, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where TEntityType : IViewModel, new() where TReturnType : new()
        {
            var task = GetAsync<TEntityType, TReturnType>(query, callerName, path, lineNumber);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<T>> GetAsync<T>(string query, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel, new()
        {
            return await GetAsync<T, T>(query, callerName, path, lineNumber).ConfigureAwait(false);
        }

        public IEnumerable<T> Get<T>(string query, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel, new()
        {
            var task = GetAsync<T>(query, callerName, path, lineNumber);
            task.Wait();
            return task.Result;
        }

        public async Task<T> GetAsync<T>(Guid id, string sessionToken = null, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel, new()
        {
            try
            {                
                var client = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var response = await client.ReadItemAsync<T>(id.ToString(), Settings.IsPartitioned ? new PartitionKey(id.ToString()) : PartitionKey.None, new ItemRequestOptions
                {
                    SessionToken = sessionToken
                }).ConfigureAwait(false);
                stopwatch.Stop();
                if(_telemetryLogger != null)
                    await _telemetryLogger.Log("GetAsync<T>", response.RequestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, id.ToString()).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return default(T);
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

        public T Get<T>(Guid id, string sessionToken = null, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel, new()
        {
            var task = GetAsync<T>(id, sessionToken, callerName, path, lineNumber);
            task.Wait();
            return task.Result;
        }
    }
}