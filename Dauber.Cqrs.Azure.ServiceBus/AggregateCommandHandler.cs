﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Exceptions;
using Dauber.Core.Time;
using HighIronRanch.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public abstract class AggregateCommandHandler<T, TCommandErrorEvent> : IAggregateCommandHandler<T> 
        where T : Command, IAggregateCommand 
        where TCommandErrorEvent : ICommandErrorEvent, new()
    {
        private readonly IServiceBusTelemetryProperties _serviceBusTelemetryProperties;
        private readonly IServiceBusWithHandlers _bus;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IDateTime _dateTime;

        protected AggregateCommandHandler(
            IServiceBusTelemetryProperties serviceBusTelemetryProperties,
            IServiceBusWithHandlers bus,
            IExceptionLogger exceptionLogger,
            IDateTime dateTime)
        {
            _serviceBusTelemetryProperties = serviceBusTelemetryProperties;
            _bus = bus;
            _exceptionLogger = exceptionLogger;
            _dateTime = dateTime;
        }

        public async Task HandleAsync(T message, ICommandActions actions)
        {
            try
            {
                AddTelemetryProperties(message);
                await ProcessAsync(message, actions);
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
                });

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
                });

                if (IsExceptionToBeRethrown(ex))
                {
                    _exceptionLogger.Log(ex);
                    throw;
                }
            }
        }

        private void AddTelemetryProperties(T message)
        {
            _serviceBusTelemetryProperties.Add("id", message.AggregateRootId.ToString());
            _serviceBusTelemetryProperties.Add("command",typeof(T).Name);
            _serviceBusTelemetryProperties.Add("message", message.ToJson());
        }

        public abstract Task ProcessAsync(T message, ICommandActions actions);

        bool IsExceptionToBeRethrown(Exception ex)
        {
            return Attribute.GetCustomAttribute(ex.GetType(), typeof(AggregateCommandRejectedAttribute)) == null;
        }
    }
}