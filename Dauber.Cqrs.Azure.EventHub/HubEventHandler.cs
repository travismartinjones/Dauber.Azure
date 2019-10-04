using System;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Exceptions;
using Dauber.Cqrs.Contracts;

namespace Dauber.Cqrs.Azure.EventHub
{
    public abstract class HubEventHandler<T> : IHubEventHandler<T> where T : IHubEvent
    {
        private readonly IExceptionLogger _exceptionLogger;
        private readonly ICorrelationEventHandler _correlationEventHandler;

        protected HubEventHandler(
            IExceptionLogger exceptionLogger,
            ICorrelationEventHandler correlationEventHandler
        )
        {
            _exceptionLogger = exceptionLogger;
            _correlationEventHandler = correlationEventHandler;
        }

        public async Task HandleAsync(T evt)
        {
            try
            {
                await ProcessAsync(evt).ConfigureAwait(false);
                
                if (evt is ICorrelationEvent correlationEvent)
                    await _correlationEventHandler.Handle(correlationEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionLogger.Log(ex);
                throw;
            }
        }

        public abstract Task ProcessAsync(T evt);
    }
}