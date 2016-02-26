using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client.TransientFaultHandling;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadWriteDocumentClientFactory
    {
        Task<IReliableReadWriteDocumentClient> GetClientAsync(IDocumentDbSettings settings);
    }
}