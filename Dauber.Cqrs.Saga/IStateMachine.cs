using System;
using System.Threading.Tasks;
using Dauber.Cqrs.Contracts;

namespace Dauber.Cqrs.Saga
{
    public interface IStateMachine<T>
    {
        Task Initialize(T state);
        Task ProcessNextStep(Guid correlationId, ICorrelationEvent evt, T state, StateMachineProcessingResults results);
    }
}