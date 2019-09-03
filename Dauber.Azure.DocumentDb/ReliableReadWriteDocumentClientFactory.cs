using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Microsoft.Azure.Cosmos;
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

        private readonly ConcurrentDictionary<string, Container> _containers = new ConcurrentDictionary<string, Container>();

        public ReliableReadWriteDocumentClientFactory(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<Container> GetClientAsync(IDocumentDbSettings settings)
        {
            var key = settings.DocumentDbRepositoryEndpointUrl + settings.DocumentDbRepositoryDatabaseId;

            if (_containers.ContainsKey(key)) return _containers[key];

            _logger.Debug(Common.LoggerContext, "Creating DocumentDb Client for {0}", settings.DocumentDbRepositoryEndpointUrl);
                
            var client = new CosmosClient(settings.DocumentDbRepositoryEndpointUrl, settings.DocumentDbRepositoryAuthKey);
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(settings.DocumentDbRepositoryDatabaseId);
            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(settings.DocumentDbRepositoryCollectionId, "/id", 400);

            _containers[key] = containerResponse.Container;
            return _containers[key];
        }        
    }
}