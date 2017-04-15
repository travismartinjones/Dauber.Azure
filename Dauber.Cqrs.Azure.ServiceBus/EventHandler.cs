using System;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Exceptions;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public abstract class EventHandler<T> : IEventHandler<T> where T : DomainEvent, IEvent
    {
        private readonly IServiceBusTelemetryProperties _serviceBusTelemetryProperties;
        private readonly IExceptionLogger _exceptionLogger;

        protected EventHandler(
            IServiceBusTelemetryProperties serviceBusTelemetryProperties,
            IExceptionLogger exceptionLogger)
        {
            _serviceBusTelemetryProperties = serviceBusTelemetryProperties;
            _exceptionLogger = exceptionLogger;
        }

        public async Task HandleAsync(T evt)
        {
            try
            {
                AddTelemetryProperties(evt);
                await ProcessAsync(evt);
            }
            catch (Exception ex)
            {
                _exceptionLogger.Log(ex);
                throw;
            }
        }

        private void AddTelemetryProperties(T evt)
        {            
            _serviceBusTelemetryProperties.Add("id", evt.Id.ToString());
            _serviceBusTelemetryProperties.Add("event", typeof(T).Name);
            _serviceBusTelemetryProperties.Add("message", evt.ToJson());
        }

        public abstract Task ProcessAsync(T evt);
    }
}