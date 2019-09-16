using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadDocumentClientFactory
    {
        Task<Container> GetClientAsync(IDocumentDbSettings settings);
    }
}