using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dauber.Core.Container;
using Dauber.Core.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Dauber.Azure.DocumentDb
{
    public static class DocumentDbExtension
    {
        /// <summary>
        /// Executes a DocumentDB query and logs it to the registered telemetry logger
        /// </summary>
        /// <typeparam name="T">An IViewModel type</typeparam>
        /// <param name="sourceQuery">The source query.</param>
        /// <param name="source">The friendly name for the caller. Used to trace the query back to the origination location in code.</param>
        /// <returns>A list of matching entities.</returns>
        public static async Task<List<T>> QueryToList<T>(this IQueryable<T> sourceQuery, string type = "QueryToList<T>", [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0)
        {
            var items = new List<T>();
            double requestCharge = 0;


            FeedIterator<T> query;

            try
            {
                query = sourceQuery.ToFeedIterator();
            }
            catch(System.ArgumentOutOfRangeException)
            {
                // if the queryable doesn't support converting to a feed iterator, return the list untimed
                return sourceQuery.ToList();
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (query.HasMoreResults)
            {
                var result =await query.ReadNextAsync().ConfigureAwait(false);
                items.AddRange(result);
                //log RU
                requestCharge += result.RequestCharge;
            }
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            
            var logger = IoC.GetInstance<ITelemetryLogger>();
            if (logger == null) return items;
            var filename = string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileNameWithoutExtension(path);
            await logger.Log(type,requestCharge, duration, callerName, filename, lineNumber, sourceQuery.ToString()).ConfigureAwait(false);
            return items;
        }
        
        /// <summary>
        /// Executes a DocumentDB query and logs it to the registered telemetry logger
        /// </summary>
        /// <typeparam name="T">An IViewModel type</typeparam>
        /// <param name="sourceQuery">The source query.</param>
        /// <param name="source">The friendly name for the caller. Used to trace the query back to the origination location in code.</param>
        /// <returns>The matching entity, or null if not found.</returns>
        public static async Task<T> QueryFirstOrDefault<T>(this IQueryable<T> sourceQuery, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0)
        {
            var query = sourceQuery.Take(1);
            var items = await QueryToList(query, "QueryFirstOrDefault<T>", callerName, path, lineNumber).ConfigureAwait(false);
            if (items == null || !items.Any())
                return default(T);

            return items.FirstOrDefault();
        }
    }
}