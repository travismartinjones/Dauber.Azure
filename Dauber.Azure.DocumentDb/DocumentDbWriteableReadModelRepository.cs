using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Contracts;
using Microsoft.Azure.Cosmos;
using static System.String;

namespace Dauber.Azure.DocumentDb
{
    public class DocumentDbWritableReadModelRepository : DocumentDbReadModelRepository, IWritableViewModelRepository
    {
        private readonly ITelemetryLogger _telemetryLogger;

        public DocumentDbWritableReadModelRepository(
            IDocumentDbSettings settings, 
            IReliableReadWriteDocumentClientFactory containerFactory, 
            ILogger logger, 
            ITelemetryLogger telemetryLogger)
            : base(settings, containerFactory, logger, telemetryLogger)
        {
            _telemetryLogger = telemetryLogger;
        }

        public WriteResponse Delete<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return DeleteAsync(item, callerName, path, lineNumber).Result;
        }

        public WriteResponse Delete<T>(IEnumerable<T> items, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return DeleteAsync<T>(items, callerName, path, lineNumber).Result;
        }

        public async Task<WriteResponse> DeleteAsync<T>(Guid[] ids, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var responses = new List<WriteResponse>();
            var tasks = ids.Select(async id =>
            {
                responses.Add(await DeleteByIdAsync<T>(container, id, callerName, path, lineNumber));
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return new WriteResponse
            {
                SessionToken = responses.FirstOrDefault()?.SessionToken
            };
        }

        private async Task<WriteResponse> DeleteByIdAsync<T>(Container container, Guid id, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            var key = id.ToString();
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var response = await container.DeleteItemAsync<T>(key, Settings.IsPartitioned ? new PartitionKey(key) : PartitionKey.None).ConfigureAwait(false);
                stopwatch.Stop();
                await _telemetryLogger.Log("DeleteByIdAsync<T>", response.RequestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, id.ToString()).ConfigureAwait(false);
                return new WriteResponse
                {
                    SessionToken = response.Headers.Session
                };
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound) return new WriteResponse();
                throw;
            }
        }

        public WriteResponse Delete<T>(Guid[] ids, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return DeleteAsync<T>(ids, callerName, path, lineNumber).Result;
        }

        public async Task<WriteResponse> DeleteAsync<T>(IEnumerable<T> items, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            if (items == null) return new WriteResponse();
            var evaluatedItems = items as T[] ?? items.ToArray();
            if(!evaluatedItems.Any()) return new WriteResponse();

            return await DeleteAsync<T>(evaluatedItems.Select(x => x.Id).ToArray(), callerName, path, lineNumber).ConfigureAwait(false);
        }

        public async Task<WriteResponse> DeleteAsync<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {            
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await container.DeleteItemAsync<T>(item.Id.ToString(), Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None, new ItemRequestOptions
            {
                IfMatchEtag = item.ETag
            }).ConfigureAwait(false);
            stopwatch.Stop();
            await _telemetryLogger.Log("DeleteAsync<T>",response.RequestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, item.Id.ToString()).ConfigureAwait(false);
            return new WriteResponse
            {
                SessionToken = response.Headers.Session
            };
        }

        public WriteResponse Insert<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return InsertAsync(item, callerName, path, lineNumber).Result;
        }

        public async Task<WriteResponse> InsertAsync<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            return await InsertAsync(container, item, callerName, path, lineNumber).ConfigureAwait(false);
        }

        public WriteResponse Insert<T>(IEnumerable<T> items, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return InsertAsync(items, callerName, path, lineNumber).Result;
        }        

        public async Task<WriteResponse> InsertAsync<T>(IEnumerable<T> items, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var responses = new List<WriteResponse>();
            foreach (var item in items)
            {
                responses.Add(await InsertAsync(container, item, callerName, path, lineNumber).ConfigureAwait(false));
            }

            return new WriteResponse
            {
                SessionToken = responses.FirstOrDefault()?.SessionToken
            };
        }

        protected async Task<WriteResponse> InsertAsync<T>(Container container, T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            // add in the entity type to allow multiple document types to share the same collection
            // this is a common cost savings technique with azure document db
            item.DocType = typeof(T).Name;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await container.CreateItemAsync(item, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None).ConfigureAwait(false);
            stopwatch.Stop();
            await _telemetryLogger.Log("InsertAsync<T>",response.RequestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, item.Id.ToString()).ConfigureAwait(false);
            return new WriteResponse
            {
                SessionToken = response.Headers.Session
            };
        }

        public WriteResponse Update<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return UpdateAsync(item, callerName, path, lineNumber).Result;
        }

        public async Task<WriteResponse> UpdateAsync<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            if (IsNullOrEmpty(item.ETag))
            {
                throw new OptimisticConcurrencyEtagMissingException($"An attempt to update {item.DocType} {item.ETag} without an ETag. If using a custom query, ensure the _etag property is included in your result set.");
            }
            
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var id = item.Id.ToString();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await container.ReplaceItemAsync(item, id, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None, new ItemRequestOptions
            {
                IfMatchEtag = item.ETag
            }).ConfigureAwait(false);
            stopwatch.Stop();
            await _telemetryLogger.Log("UpdateAsync<T>",response.RequestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, id).ConfigureAwait(false);
            return new WriteResponse
            {
                SessionToken = response.Headers.Session
            };
        }                

        public WriteResponse Upsert<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return UpsertAsync(item, callerName, path, lineNumber).Result;
        }

        public async Task<WriteResponse> UpsertAsync<T>(T item, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            return await UpsertAsync(container, item, callerName, path, lineNumber).ConfigureAwait(false);
        }

        public WriteResponse Upsert<T>(IEnumerable<T> items, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            return UpsertAsync(items, callerName, path, lineNumber).Result;
        }

        public async Task<WriteResponse> UpsertAsync<T>(IEnumerable<T> items, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) where T : IViewModel
        {
            var container = await ClientFactory.GetContainerAsync(Settings).ConfigureAwait(false);
            var responses = new List<WriteResponse>();
            foreach (var item in items)
            {
                responses.Add(await UpsertAsync(container, item, callerName, path, lineNumber).ConfigureAwait(false));
            }

            return new WriteResponse
            {
                SessionToken = responses.FirstOrDefault()?.SessionToken
            };
        }

        protected async Task<WriteResponse> UpsertAsync<T>(Container container, T item, string callerName, string path, int lineNumber) where T : IViewModel
        {
            item.DocType = typeof(T).Name;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await container.UpsertItemAsync(item, Settings.IsPartitioned ? new PartitionKey(item.Id.ToString()) : PartitionKey.None, new ItemRequestOptions
            {
                IfMatchEtag = item.ETag
            }).ConfigureAwait(false);
            stopwatch.Stop();
            await _telemetryLogger.Log("UpsertAsync<T>",response.RequestCharge, stopwatch.ElapsedMilliseconds, callerName, GetFilename(path), lineNumber, item.Id.ToString()).ConfigureAwait(false);
            return new WriteResponse
            {
                SessionToken = response.Headers.Session
            };
        }
    }
}