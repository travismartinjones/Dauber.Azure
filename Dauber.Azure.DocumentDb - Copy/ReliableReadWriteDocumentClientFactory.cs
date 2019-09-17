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
    /// </summary>
    public class ReliableReadWriteDocumentClientFactory : IReliableReadWriteDocumentClientFactory
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, Container> _containers = new ConcurrentDictionary<string, Container>();

        public ReliableReadWriteDocumentClientFactory(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<Container> GetContainerAsync(IDocumentDbSettings settings)
        {
            var key = settings.DocumentDbRepositoryEndpointUrl + settings.DocumentDbRepositoryDatabaseId;

            if (_containers.ContainsKey(key)) return _containers[key];

            lock(_containers)
            {
                if (_containers.ContainsKey(key)) return _containers[key];

                _logger.Debug(Common.LoggerContext, "Creating DocumentDb Client for {0}", settings.DocumentDbRepositoryEndpointUrl);
                    
                var client = new CosmosClient(settings.DocumentDbRepositoryEndpointUrl, settings.DocumentDbRepositoryAuthKey);            
                var database = client.GetDatabase(settings.DocumentDbRepositoryDatabaseId);            
                var container = database.GetContainer(settings.DocumentDbRepositoryCollectionId);

                _containers[key] = container;
                return _containers[key];
            }
        }
    }
}