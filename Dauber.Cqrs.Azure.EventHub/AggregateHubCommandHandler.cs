using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Exceptions;
using Dauber.Core.Time;
using Dauber.Cqrs.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus;

namespace Dauber.Cqrs.Azure.EventHub
{
    public abstract class AggregateHubCommandHandler<T, TCommandErrorEvent> : IAggregateHubCommandHandler<T> 
        where T : HubCommand, IAggregateHubCommand
        where TCommandErrorEvent : ICommandErrorEvent, new()
    {
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IDateTime _dateTime;
        private IServiceBusWithHandlers _bus;

        protected AggregateHubCommandHandler(
            IServiceBusWithHandlers bus,
            IExceptionLogger exceptionLogger,
            IDateTime dateTime)
        {
            _bus = bus;
            _exceptionLogger = exceptionLogger;
            _dateTime = dateTime;
        }

        public async Task HandleAsync(T message)
        {
            try
            {
                await ProcessAsync(message).ConfigureAwait(false);
            }
            catch (AggregateException exceptions)
            {
                var userId = (Guid?)typeof(T).GetProperty("UserId")?.GetValue(message);
                
                await _bus.PublishAsync(new TCommandErrorEvent
                {
                    Id = message.AggregateRootId,
                    CommandMessageId = message.MessageId,
                    CommandName = typeof(T).Name,
                    EventDate = _dateTime.UtcNow,
                    Errors = exceptions.InnerExceptions.Select(x => x.Message).ToList(),
                    UserId = userId
                }).ConfigureAwait(false);

                if (exceptions.InnerExceptions.Any(IsExceptionToBeRethrown))
                {
                    _exceptionLogger.Log(exceptions);
                    throw;
                }
            }
            catch (Exception ex)
            {                
                var userId = (Guid?)typeof(T).GetProperty("UserId")?.GetValue(message);
                await _bus.PublishAsync(new TCommandErrorEvent
                {
                    Id = message.AggregateRootId,
                    CommandMessageId = message.MessageId,
                    CommandName = typeof(T).Name,
                    EventDate = _dateTime.UtcNow,
                    Errors = new List<string> { ex.Message },
                    UserId = userId
                }).ConfigureAwait(false);

                if (IsExceptionToBeRethrown(ex))
                {
                    _exceptionLogger.Log(ex);
                    throw;
                }
            }
        }

        public abstract Task ProcessAsync(T message);

        bool IsExceptionToBeRethrown(Exception ex)
        {
            return Attribute.GetCustomAttribute(ex.GetType(), typeof(AggregateCommandRejectedAttribute)) == null;
        }
    }
}