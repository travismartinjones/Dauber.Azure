using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dauber.Azure.EventHub;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Exceptions;
using ILogger = Dauber.Core.ILogger;

namespace Dauber.Azure.EventHub
{
    public class EventHubServiceBuilder
    {
        private static string Context = "EventHubServiceBuilder";
        private readonly IHandlerActivator _handlerActivator;
        private readonly IEventHubSettings _settings;
        private readonly ILogger _logger;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IHubEventErrorBus _hubEventErrorBus;
        private readonly IHubCommandErrorBus _hubCommandErrorBus;
        private IEnumerable<string> _messageAssembliesToScan;
        private IEnumerable<Type> _messageTypes; 

        private IEnumerable<string> _messageHandlerAssembliesToScan;
        private IEnumerable<Type> _messageHandlerTypes;

        private IEnumerable<string> _eventAssembliesToScan;
        private IEnumerable<Type> _eventTypes;

        private IEnumerable<string> _eventHandlerAssembliesToScan;
        private IEnumerable<Type> _eventHandlerTypes;

        public EventHubServiceBuilder(
            IHandlerActivator handlerActivator,
            IEventHubSettings settings,
            ILogger logger,
            IExceptionLogger exceptionLogger,
            IHubEventErrorBus hubEventErrorBus,
            IHubCommandErrorBus hubCommandErrorBus)
        {
            _handlerActivator = handlerActivator;
            _settings = settings;
            _logger = logger;
            _exceptionLogger = exceptionLogger;
            _hubEventErrorBus = hubEventErrorBus;
            _hubCommandErrorBus = hubCommandErrorBus;
        }

        public EventHubServiceBuilder WithCommandsInAssemblies(IEnumerable<string> assembliesToScan)
        {
            _messageAssembliesToScan = assembliesToScan;
            return this;
        }

        public EventHubServiceBuilder WithCommands(IEnumerable<Type> commandTypes)
        {
            _messageTypes = commandTypes;
            return this;
        }

        public EventHubServiceBuilder WithCommandHandlersInAssemblies(IEnumerable<string> assembliesToScan)
        {
            _messageHandlerAssembliesToScan = assembliesToScan;
            return this;
        }

        public EventHubServiceBuilder WithCommandHandlers(IEnumerable<Type> handlerTypes)
        {
            _messageHandlerTypes = handlerTypes;
            return this;
        }

        public EventHubServiceBuilder WithEventsInAssemblies(IEnumerable<string> assembliesToScan)
        {
            _eventAssembliesToScan = assembliesToScan;
            return this;
        }

        public EventHubServiceBuilder WithEvents(IEnumerable<Type> commandTypes)
        {
            _eventTypes = commandTypes;
            return this;
        }

        public EventHubServiceBuilder WithEventHandlersInAssemblies(IEnumerable<string> assembliesToScan)
        {
            _eventHandlerAssembliesToScan = assembliesToScan;
            return this;
        }

        public EventHubServiceBuilder WithEventHandlers(IEnumerable<Type> handlerTypes)
        {
            _eventHandlerTypes = handlerTypes;
            return this;
        }

        public async Task<EventHubService> BuildAsync()
        {
            var hubService = new EventHubService(_handlerActivator, _exceptionLogger, _settings, _hubEventErrorBus, _hubCommandErrorBus);

            CreateHandledQueuesInAssembliesAsync(hubService);
            CreateSpecificHandledQueuesAsync(hubService);
            CreateQueuesInAssembliesAsync(hubService);
            CreateSpecificQueuesAsync(hubService);
            CreateHandledEventsInAssembliesAsync(hubService);
            CreateSpecificHandledEventsAsync(hubService);
            CreateEventsInAssembliesAsync(hubService);
            CreateSpecificEventsAsync(hubService);

            await hubService.Start().ConfigureAwait(false);

            return hubService;
        }

        private void CreateHandledQueuesInAssembliesAsync(EventHubService service)
        {
            if (_messageHandlerAssembliesToScan == null) return;
            
            var assemblies = GetAssemblies(_messageHandlerAssembliesToScan);

            var handlerTypesInAssemblies = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.DoesTypeImplementInterface(typeof(IHubCommandHandler<>)))
                .ToList();

            var found = string.Join(",", handlerTypesInAssemblies.Select(e => e.Name));
            _logger.Debug(Context, $"Found the following hub command handlers: {found}");

            CreateHandledQueues(handlerTypesInAssemblies, service);
        }

        private void CreateHandledQueues(IEnumerable<Type> handlerTypes, EventHubService service)
        {
            foreach (var handlerType in handlerTypes)
            {
                service.RegisterCommandHandler(handlerType);
            }
        }

        private void CreateSpecificHandledQueuesAsync(EventHubService service)
        {
            if (_messageHandlerTypes == null) return;
            CreateHandledQueuesAsync(_messageHandlerTypes, service);
        }

        private void CreateHandledQueuesAsync(IEnumerable<Type> handlerTypes, EventHubService service)
        {
            foreach (var handlerType in handlerTypes)
            {
                service.RegisterCommandHandler(handlerType);
            }
        }

        private void CreateQueuesInAssembliesAsync(EventHubService service)
        {
            if (_messageAssembliesToScan == null) return;

            var assemblies = GetAssemblies(_messageAssembliesToScan);

            var commandTypesInAssemblies = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.DoesTypeImplementInterface(typeof (IHubCommand)))
                .ToList();

            var found = string.Join(",", commandTypesInAssemblies.Select(e => e.Name));
            _logger.Debug(Context, $"Found the following hub commands: {found}");

            CreateQueuesAsync(commandTypesInAssemblies, service);
        }

        private void CreateQueuesAsync(IEnumerable<Type> commandTypes, EventHubService service)
        {
            foreach (var commandType in commandTypes)
            {
                service.RegisterCommand(commandType);
            }
        }

        private void CreateSpecificQueuesAsync(EventHubService service)
        {
            if (_messageTypes == null) return;
            CreateQueuesAsync(_messageTypes, service);
        }

        private void CreateHandledEventsInAssembliesAsync(EventHubService service)
        {
            if (_eventHandlerAssembliesToScan == null) return;
            var assemblies = GetAssemblies(_eventHandlerAssembliesToScan);

            var eventHandlerTypesInAssemblies = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.DoesTypeImplementInterface(typeof(IHubEventHandler<>)))
                .ToList();

            var found = string.Join(",", eventHandlerTypesInAssemblies.Select(e => e.Name));
            _logger.Debug(Context, $"Found the following hub event handlers: {found}");

            CreateHandledEventsAsync(eventHandlerTypesInAssemblies, service);
        }

        private void CreateHandledEventsAsync(IEnumerable<Type> eventHandlerTypes, EventHubService service)
        {
            foreach (var eventType in eventHandlerTypes)
            {
                service.RegisterEventHandler(eventType);
            }
        }

        private void CreateSpecificHandledEventsAsync(EventHubService service)
        {
            if (_eventHandlerTypes == null) return;
            CreateHandledEventsAsync(_eventHandlerTypes, service);
        }

        private void CreateEventsInAssembliesAsync(EventHubService service)
        {
            if (_eventAssembliesToScan == null) return;
            var assemblies = GetAssemblies(_eventAssembliesToScan);

            var eventTypesInAssemblies = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.DoesTypeImplementInterface(typeof(IHubEvent)))
                .ToList();

            var found = string.Join(",", eventTypesInAssemblies.Select(e => e.Name));
            _logger.Debug(Context, $"Found the following hub events: {found}");

            CreateEventsAsync(eventTypesInAssemblies, service);
        }

        private void CreateEventsAsync(IEnumerable<Type> eventTypes, EventHubService service)
        {
            foreach (var eventType in eventTypes)
            {
                service.RegisterEvent(eventType);
            }
        }

        private void CreateSpecificEventsAsync(EventHubService service)
        {
            if (_eventTypes == null) return;
            CreateEventsAsync(_eventTypes, service);
        }

        private IEnumerable<Assembly> GetAssemblies(IEnumerable<string> assemblies)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(assembly => assemblies.Contains(assembly.GetName().Name));
        } 
    }
}