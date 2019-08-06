using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadWriteDocumentClientFactory
    {
        Task<DocumentClient> GetClientAsync(IDocumentDbSettings settings);
    }
}