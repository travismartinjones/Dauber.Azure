using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Dauber.Azure.DocumentDb
{
    /// <summary>
    /// Make this a singleton.
    /// </summary>\\Mac\Home\Documents\Development\dauber-site\Dauber.Azure.DocumentDb\ReliableReadWriteDocumentClientFactory.cs
    public class ReliableReadWriteDocumentClientFactory : IReliableReadWriteDocumentClientFactory
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, DocumentClient> _clients = new ConcurrentDictionary<string, DocumentClient>();

        public ReliableReadWriteDocumentClientFactory(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<DocumentClient> GetClientAsync(IDocumentDbSettings settings)
        {
            var key = settings.DocumentDbRepositoryEndpointUrl + settings.DocumentDbRepositoryDatabaseId;

            if (_clients.ContainsKey(key)) return _clients[key];

            _logger.Debug(Common.LoggerContext, "Creating DocumentDb Client for {0}", settings.DocumentDbRepositoryEndpointUrl);
                
            var client = new DocumentClient(new Uri(settings.DocumentDbRepositoryEndpointUrl), settings.DocumentDbRepositoryAuthKey);            
            await client.OpenAsync().ConfigureAwait(false);

            await SpinUpDatabaseAsync(client, settings.DocumentDbRepositoryDatabaseId).ConfigureAwait(false);

            _clients[key] = client;
            return client;
        }

        private async Task SpinUpDatabaseAsync(DocumentClient client, string databaseId)
        {
            var x = client.CreateDatabaseQuery()
                .Where(d => d.Id == databaseId)
                .AsEnumerable()
                .FirstOrDefault();

            if (x == null)
            {
                _logger.Debug(Common.LoggerContext, "Create DocumentDb database for {0}", databaseId);

                await client.CreateDatabaseAsync(new Database { Id = databaseId }).ConfigureAwait(false);
            }
        }
    }
}