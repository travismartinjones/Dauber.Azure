using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadDocumentClientFactory
    {
        Task<DocumentClient> GetClientAsync(IDocumentDbSettings settings);
    }
}