﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Exceptions;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;
using ILogger = Dauber.Core.ILogger;

namespace Dauber.Azure.EventHub
{
    public class EventHubProcessor : IEventProcessor
    {
        private readonly IEventHubSettings _settings;
        private readonly IHandlerActivator _handlerActivator;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IHubEventErrorBus _hubEventErrorBus;
        private readonly IHubCommandErrorBus _hubCommandErrorBus;
        private readonly IDictionary<Type, Type> _commandHandlers;
        private readonly IDictionary<Type, ISet<Type>> _eventHandlers;
        private readonly ILogger _logger;

        public EventHubProcessor(IEventHubSettings settings,
            IHandlerActivator handlerActivator,
            IExceptionLogger exceptionLogger,
            IHubEventErrorBus hubEventErrorBus,
            IHubCommandErrorBus hubCommandErrorBus,
            IDictionary<Type, Type> commandHandlers,
            IDictionary<Type, ISet<Type>> eventHandlers, 
            ILogger logger)
        {
            _settings = settings;
            _handlerActivator = handlerActivator;
            _exceptionLogger = exceptionLogger;
            _hubEventErrorBus = hubEventErrorBus;
            _hubCommandErrorBus = hubCommandErrorBus;
            _commandHandlers = commandHandlers;
            _eventHandlers = eventHandlers;
            _logger = logger;
        }

        public Task OpenAsync(PartitionContext context)
        {
            _logger.Information(nameof(EventHubProcessor), $"Opening {context.ConsumerGroupName} {context.EventHubPath} {context.PartitionId}");
            return Task.CompletedTask;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            _logger.Information(nameof(EventHubProcessor), $"Closing {context.ConsumerGroupName} {context.EventHubPath} {context.PartitionId} {reason}");
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var message in messages)
            {
                if (context.CancellationToken.IsCancellationRequested) return;
                await ProcessEventAsync(message).ConfigureAwait(false);
            }
            
            if (context.CancellationToken.IsCancellationRequested) return;
            await context.CheckpointAsync().ConfigureAwait(false);
        }

        private async Task ProcessEventAsync(EventData eventData)
        {
            var messageType = Type.GetType((string)eventData.Properties["ContentType"]);
            if (messageType == null) return;
            var json = Encoding.UTF8.GetString(eventData.Body.Array);
            var message = JsonConvert.DeserializeObject(json, messageType);
            
            if (message is IHubEvent hubEvent)
            {
                try
                {
                    await ProcessHubEvent(messageType, hubEvent).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    // on any failure, pass over the message to an error handling bus (likely azure service bus)
                    // event hub doesn't have dead-lettering and passes error handling and poising message
                    // handling off to the partition clients, instead, defer that responsibility to a bus that 
                    // has support for these mechanisms
                    _exceptionLogger.Log(ex);
                    hubEvent.IsHubError = true;

                    if (!_settings.IsFallbackToServiceBusEnabled) return;
                    var attribute = ((EventHubAttribute) Attribute.GetCustomAttribute(hubEvent.GetType(), typeof(EventHubAttribute)));
                    if (!(attribute?.IsFallbackToServiceBusEnabled ?? false)) return;
                    await _hubEventErrorBus.ProcessAsync(hubEvent).ConfigureAwait(false);
                }
            } 
            else if (message is IHubCommand hubCommand)
            {
                try
                {
                    await ProcessHubCommand(messageType, hubCommand).ConfigureAwait(false);
                    
                }
                catch(Exception ex)
                {
                    // on any failure, pass over the message to an error handling bus (likely azure service bus)
                    // event hub doesn't have dead-lettering and passes error handling and poising message
                    // handling off to the partition clients, instead, defer that responsibility to a bus that 
                    // has support for these mechanisms
                    _exceptionLogger.Log(ex);
                    
                    if (!_settings.IsFallbackToServiceBusEnabled) return;
                    var attribute = ((EventHubAttribute) Attribute.GetCustomAttribute(hubCommand.GetType(), typeof(EventHubAttribute)));
                    if (!(attribute?.IsFallbackToServiceBusEnabled ?? false)) return;
                    await _hubCommandErrorBus.ProcessAsync(hubCommand).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessHubCommand(Type messageType, IHubCommand hubCommand)
        {
            var handlerType = _commandHandlers[messageType];
            var handler = _handlerActivator.GetInstance(handlerType);
            var handleMethodInfo = handlerType.GetMethod("HandleAsync");
            if (handleMethodInfo == null) return;
            await ((Task) handleMethodInfo?.Invoke(handler, new[] {hubCommand})).ConfigureAwait(false);
        }

        private async Task ProcessHubEvent(Type messageType, IHubEvent hubEvent)
        {
            var handlerTypes = _eventHandlers[messageType];

            foreach (var handlerType in handlerTypes)
            {
                var handler = _handlerActivator.GetInstance(handlerType);
                var handleMethodInfo = handlerType.GetMethod("HandleAsync");
                if (handleMethodInfo == null) return;
                await ((Task) handleMethodInfo?.Invoke(handler, new[] {hubEvent})).ConfigureAwait(false);
            }
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            _exceptionLogger?.Log(error);
            return Task.CompletedTask;
        }
    }
}