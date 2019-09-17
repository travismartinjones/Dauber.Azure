using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadDocumentClientFactory
    {
        Task<Container> GetClientAsync(IDocumentDbSettings settings);
    }
}