using System;
using System.Threading.Tasks;

namespace Dauber.Cqrs.Contracts
{
    public interface ISagaStateMachineEventDispatcher
    {        
        Task<StateMachineProcessingResults> OnEventAsync(Type stateType, object state, ICorrelationEvent evt);
    }
}