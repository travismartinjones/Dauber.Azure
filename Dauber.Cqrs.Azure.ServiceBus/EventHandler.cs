using System;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Exceptions;
using Dauber.Cqrs.Contracts;
using HighIronRanch.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public abstract class EventHandler<T> : IEventHandler<T> where T : IEvent
    {
        private readonly IServiceBusTelemetryProperties _serviceBusTelemetryProperties;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly ICorrelationEventHandler _correlationEventHandler;

        protected EventHandler(
            IServiceBusTelemetryProperties serviceBusTelemetryProperties,
            IExceptionLogger exceptionLogger,
            ICorrelationEventHandler correlationEventHandler
            )
        {
            _serviceBusTelemetryProperties = serviceBusTelemetryProperties;
            _exceptionLogger = exceptionLogger;
            _correlationEventHandler = correlationEventHandler;
        }

        public async Task HandleAsync(T evt)
        {
            try
            {
                AddTelemetryProperties(evt);
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

        private void AddTelemetryProperties(T evt)
        {
            if (_serviceBusTelemetryProperties == null) return;

            if (typeof(T).IsSubclassOf(typeof(DomainEvent)))
            {
                var domainEvent = (DomainEvent) (object) evt;
                _serviceBusTelemetryProperties.Add("id", domainEvent?.Id.ToString() ?? "Unknown");
            }

            _serviceBusTelemetryProperties.Add("event", typeof(T).Name);
            _serviceBusTelemetryProperties.Add("message", evt?.ToJson() ?? "{}");
        }

        public abstract Task ProcessAsync(T evt);
    }
}