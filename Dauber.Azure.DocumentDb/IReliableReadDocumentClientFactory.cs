using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Client.TransientFaultHandling;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadDocumentClientFactory
    {
        Task<IReliableReadDocumentClient> GetClientAsync(IDocumentDbSettings settings);
    }
}