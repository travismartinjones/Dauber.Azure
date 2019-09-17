using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadWriteDocumentClientFactory
    {
        Task<Container> GetContainerAsync(IDocumentDbSettings settings);
    }
}