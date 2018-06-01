using System;
using System.Threading.Tasks;

namespace Dauber.Cqrs.Contracts
{
    public interface ISagaStateReadRepository
    {
        Task<SagaState> Read(Guid correlationId);
    }
}