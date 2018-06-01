using System;
using System.Threading.Tasks;

namespace Dauber.Cqrs.Contracts
{
    public interface ISagaAction<in TState,TResult>
    {
        Task<TResult> Handle(Guid corrolationId, ICorrelationEvent evt, TState state);
    }
}