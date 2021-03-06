﻿using System;
using System.Collections.Generic;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Exceptions;
using Microsoft.Azure.EventHubs.Processor;
using ILogger = Dauber.Core.ILogger;

namespace Dauber.Azure.EventHub
{
    public class EventProcessorFactory : IEventProcessorFactory
    {
        private readonly IEventHubSettings _settings;
        private readonly IHandlerActivator _handlerActivator;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IHubEventErrorBus _hubEventErrorBus;
        private readonly IHubCommandErrorBus _hubCommandErrorBus;
        private readonly IDictionary<Type, Type> _commandHandlers;
        private readonly IDictionary<Type, ISet<Type>> _eventHandlers;
        private readonly ILogger _logger;

        public EventProcessorFactory(IEventHubSettings settings,
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

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventHubProcessor(_settings, _handlerActivator, _exceptionLogger, _hubEventErrorBus, _hubCommandErrorBus, _commandHandlers, _eventHandlers, _logger);
        }
    }
}