using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Client.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Dauber.Azure.DocumentDb
{
    /// <summary>
    /// Make this a singleton.
    /// </summary>\\Mac\Home\Documents\Development\dauber-site\Dauber.Azure.DocumentDb\ReliableReadWriteDocumentClientFactory.cs
    public class ReliableReadWriteDocumentClientFactory : IReliableReadWriteDocumentClientFactory
    {
        private readonly ILogger _logger;

        private readonly IDictionary<string, IReliableReadWriteDocumentClient> _clients = new Dictionary<string, IReliableReadWriteDocumentClient>();

        public ReliableReadWriteDocumentClientFactory(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IReliableReadWriteDocumentClient> GetClientAsync(IDocumentDbSettings settings)
        {
            var key = settings.DocumentDbRepositoryEndpointUrl + settings.DocumentDbRepositoryDatabaseId;

            if (_clients.ContainsKey(key)) return _clients[key];

            _logger.Debug(Common.LoggerContext, "Creating DocumentDb Client for {0}", settings.DocumentDbRepositoryEndpointUrl);
                
            var client = new DocumentClient(new Uri(settings.DocumentDbRepositoryEndpointUrl), settings.DocumentDbRepositoryAuthKey)
                .AsReliable(new FixedInterval(10, TimeSpan.FromSeconds(1)));
            await client.OpenAsync();

            await SpinUpDatabaseAsync(client, settings.DocumentDbRepositoryDatabaseId);

            _clients[key] = client;
            return client;
        }

        private async Task SpinUpDatabaseAsync(IReliableReadWriteDocumentClient client, string databaseId)
        {
            var x = client.CreateDatabaseQuery()
                .Where(d => d.Id == databaseId)
                .AsEnumerable()
                .FirstOrDefault();

            if (x == null)
            {
                _logger.Debug(Common.LoggerContext, "Create DocumentDb database for {0}", databaseId);

                await client.CreateDatabaseAsync(new Database { Id = databaseId });
            }
        }
    }
}