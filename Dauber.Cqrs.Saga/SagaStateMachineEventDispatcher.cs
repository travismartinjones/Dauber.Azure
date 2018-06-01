using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dauber.Core.Container;
using Dauber.Cqrs.Contracts;
using Newtonsoft.Json;

namespace Dauber.Cqrs.Saga
{
    public class SagaStateMachineEventDispatcher : ISagaStateMachineEventDispatcher
    {
        private readonly IDictionary<Type, Type> _stateMachineTypes;
        private readonly IContainer _container;

        public SagaStateMachineEventDispatcher(
            IDictionary<Type, Type> stateMachineTypes,
            IContainer container)
        {
            _stateMachineTypes = stateMachineTypes;
            _container = container;
        }

        public async Task<StateMachineProcessingResults> OnEventAsync(Type stateType, object state, ICorrelationEvent evt)
        {
            if(_stateMachineTypes == null)
                throw new Exception("Saga configuration error. You need to configure the SagaStateMachineFactoryBuilder.");

            var stateMachineType = _stateMachineTypes[stateType];

            if (stateMachineType == null)
            {
                if (_stateMachineTypes.Values.Count == 0)
                    throw new Exception($"State machine type {stateType} not found. No state machines have been scanned. Did you add the assembly via SagaStateMachineFactoryBuilder.WithStateMachinesInAssembiles?");
                throw new Exception($"State machine type {stateType} not found out of {_stateMachineTypes.Values.Count} state machines.");
            }

            if(_container == null)
                throw new Exception("IContainer supplied to SagaStateMachineFactoryBuilder is null.");

            var stateMachine = _container.GetInstance(stateMachineType);
            var methodInfo = stateMachineType.GetMethod("ProcessNextStep", new[] { typeof(Guid), typeof(ICorrelationEvent), stateType, typeof(StateMachineProcessingResults) });
            
            if (methodInfo == null || stateMachine == null)
                throw new Exception($"No valid state machine {stateMachineType.Name} for state {stateType.Name}.");
            {
                var result = new StateMachineProcessingResults();
                await ((Task) methodInfo?.Invoke(stateMachine, new[] {evt.CorrelationId, evt, state, result}));
                return result;
            }            
        }
    }
}