using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace Dauber.Azure.DocumentDb
{
    public interface IReliableReadDocumentClientFactory
    {
        Task<IDocumentClient> GetClientAsync(IDocumentDbSettings settings);
    }
}