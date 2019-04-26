using System;
using System.Threading.Tasks;
using Dauber.Cqrs.Contracts;

namespace Dauber.Cqrs.Saga
{
    public abstract class StateMachine<T> : IStateMachine<T>
    {
        private readonly ISagaStateRepository _repository;

        protected StateMachine(
            ISagaStateRepository repository
            )
        {
            _repository = repository;
        }

        public async Task Initialize(T state)
        {
            var correlationId = Guid.NewGuid();
            await _repository.Create(correlationId, state).ConfigureAwait(false);

            // create an event to represent the new state of the state machine, having an object instead of null
            // avoids having to perform a null check in each state machine
            var stateMachineCreatedEvent = new StateMachineCreatedEvent {CorrelationId = correlationId};
            var firstStepResult = new StateMachineProcessingResults();
            await ProcessNextStep(correlationId, stateMachineCreatedEvent, state, firstStepResult).ConfigureAwait(false);

            if (firstStepResult.IsSagaComplete)
            {
                // even though crazy, handle if a saga of a single command has been created
                await _repository.Delete(correlationId).ConfigureAwait(false);
            }
            else
            {
                await _repository.Update(correlationId, state).ConfigureAwait(false);
            }
        }

        public abstract Task ProcessNextStep(Guid correlationId, ICorrelationEvent evt, T state, StateMachineProcessingResults results);
    }
}