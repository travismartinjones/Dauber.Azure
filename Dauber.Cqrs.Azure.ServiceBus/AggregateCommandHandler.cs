using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core.Time;
using HighIronRanch.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public abstract class AggregateCommandHandler<T, TCommandErrorEvent> : IAggregateCommandHandler<T> 
        where T : Command, IAggregateCommand 
        where TCommandErrorEvent : ICommandErrorEvent, new()
    {
        private readonly IServiceBusWithHandlers _bus;
        private readonly IDateTime _dateTime;

        protected AggregateCommandHandler(
            IServiceBusWithHandlers bus,
            IDateTime dateTime)
        {
            _bus = bus;
            _dateTime = dateTime;
        }

        public async Task HandleAsync(T message, ICommandActions actions)
        {
            try
            {
                await ProcessAsync(message, actions);
            }
            catch (AggregateException exceptions)
            {
                await _bus.PublishAsync(new TCommandErrorEvent
                {
                    Id = message.AggregateRootId,
                    CommandMessageId = message.MessageId,
                    CommandName = typeof(T).Name,
                    EventDate = _dateTime.UtcNow,
                    Errors = exceptions.InnerExceptions.Select(x => x.Message).ToList()
                });

                if(exceptions.InnerExceptions.Any(IsExceptionToBeRethrown))
                    throw;
            }
            catch (Exception ex)
            {                
                await _bus.PublishAsync(new TCommandErrorEvent
                {
                    Id = message.AggregateRootId,
                    CommandMessageId = message.MessageId,
                    CommandName = typeof(T).Name,
                    EventDate = _dateTime.UtcNow,
                    Errors = new List<string> { ex.Message }
                });

                if(IsExceptionToBeRethrown(ex))
                    throw;
            }
        }

        public abstract Task ProcessAsync(T message, ICommandActions actions);

        bool IsExceptionToBeRethrown(Exception ex)
        {
            return Attribute.GetCustomAttribute(ex.GetType(), typeof(AggregateCommandRejectedAttribute)) == null;
        }
    }
}