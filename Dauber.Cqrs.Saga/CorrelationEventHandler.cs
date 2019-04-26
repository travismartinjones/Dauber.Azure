using System.Threading.Tasks;
using Dauber.Cqrs.Contracts;

namespace Dauber.Cqrs.Saga
{
    public class CorrelationEventHandler : ICorrelationEventHandler
    {
        private readonly ISagaStateRepository _sagaStateRepository;
        private readonly ISagaStateMachineEventDispatcher _eventDispatcher;

        public CorrelationEventHandler(
            ISagaStateRepository sagaStateRepository,
            ISagaStateMachineEventDispatcher eventDispatcher
        )
        {
            _sagaStateRepository = sagaStateRepository;
            _eventDispatcher = eventDispatcher;
        }

        public async Task Handle(ICorrelationEvent evt)
        {
            if (!evt.CorrelationId.HasValue) return;

            var sagaState = await _sagaStateRepository.Read(evt.CorrelationId.Value).ConfigureAwait(false);

            if (sagaState == null) return;

            var result = await _eventDispatcher.OnEventAsync(sagaState.StateType, sagaState.State, evt).ConfigureAwait(false);

            if (result == null) return;

            if (!result.IsEventHandled) return;

            if (result.IsSagaComplete)
                await _sagaStateRepository.Delete(evt.CorrelationId.Value).ConfigureAwait(false);
            else
                await _sagaStateRepository.Update(evt.CorrelationId.Value, sagaState.State).ConfigureAwait(false);
        }
    }
}