using System;
using System.Threading.Tasks;

namespace Dauber.Cqrs.Contracts
{
    public interface ISagaStateRepository : ISagaStateReadRepository
    {        
        Task Create(Guid correlationId, object state);        
        Task Update(Guid correlationId, object state);
        Task Delete(Guid correlationId);
    }
}