using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core;
using Dauber.Core.Exceptions;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;

namespace Dauber.Azure.EventHub
{
    public class EventHubService : IEventHubService
    {
        private readonly IHandlerActivator _handlerActivator;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IEventHubSettings _settings;
        private readonly IHubEventErrorBus _hubEventErrorBus;
        private readonly IHubCommandErrorBus _hubCommandErrorBus;
        private IDictionary<Type, EventProcessorHost> _eventProcessorHosts = new ConcurrentDictionary<Type, EventProcessorHost>();
        private IDictionary<Type, EventHubClient> _eventClients = new ConcurrentDictionary<Type, EventHubClient>();
        private IDictionary<Type, EventHubClient> _commandClients = new ConcurrentDictionary<Type, EventHubClient>();
        private readonly IDictionary<Type, Type> _commandHandlers = new ConcurrentDictionary<Type, Type>();
        private readonly IDictionary<Type, ISet<Type>> _eventHandlers = new ConcurrentDictionary<Type, ISet<Type>>();

        public EventHubService(
            IHandlerActivator handlerActivator, 
            IExceptionLogger exceptionLogger, 
            IEventHubSettings settings,
            IHubEventErrorBus hubEventErrorBus,
            IHubCommandErrorBus hubCommandErrorBus)
        {
            _handlerActivator = handlerActivator;
            _exceptionLogger = exceptionLogger;
            _settings = settings;
            _hubEventErrorBus = hubEventErrorBus;
            _hubCommandErrorBus = hubCommandErrorBus;
        }

        public async Task SendAsync(IHubCommand command)
        {
            var eventHubClient = _commandClients[command.GetType()];
            var eventData = new EventData(Encoding.UTF8.GetBytes(command.ToJson()));
            eventData.Properties["ContentType"] = command.GetType().AssemblyQualifiedName;
            var partitionKey = command is IAggregateHubCommand aggregateCommand ? aggregateCommand.GetAggregateId() : command.MessageId.ToString();
            await eventHubClient.SendAsync(eventData, partitionKey).ConfigureAwait(false);
        }

        public async Task PublishAsync(IHubEvent evt)
        {
            var eventHubClient = _eventClients[evt.GetType()];
            var eventData = new EventData(Encoding.UTF8.GetBytes(evt.ToJson()));
            eventData.Properties["ContentType"] = evt.GetType().AssemblyQualifiedName;
            var partitionKey = evt is IAggregateHubEvent aggregateCommand ? aggregateCommand.GetAggregateId() : evt.MessageId.ToString();
            await eventHubClient.SendAsync(eventData, partitionKey).ConfigureAwait(false);
        }

        public void RegisterEventHandler(Type handlerType)
        {
            if (_eventHandlers.Values.Any(v => v.Contains(handlerType))) return;
            var interfaces = handlerType.GetInterfaces().Where(i => i.IsGenericType);
            var eventTypes = interfaces.Select(i => i.GetGenericArguments()[0]).Distinct();

            foreach (var eventType in eventTypes)
            {
                RegisterEventProcessorHost(eventType);
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].Add(handlerType);
                }
                else
                {
                    _eventHandlers.Add(eventType, new HashSet<Type>() { handlerType });
                }
            }
        }

        public void RegisterCommandHandler(Type handlerType)
        {
            if (_commandHandlers.Values.Contains(handlerType))
                return;

            var interfaces = handlerType.GetInterfaces().Where(i => i.IsGenericType);
            var messageTypes = interfaces.Select(i => i.GetGenericArguments()[0]).Distinct();

            foreach (var messageType in messageTypes)
            {
                RegisterEventProcessorHost(messageType);
                _commandHandlers.Add(messageType, handlerType);
            }
        }

        private void RegisterEventProcessorHost(Type messageType)
        {
            if (_eventProcessorHosts.ContainsKey(messageType)) return;
            var eventProcessorHost = new EventProcessorHost(
                messageType.GetEventHubName(),
                messageType.IsAssignableFrom(typeof(IHubEvent)) ? _settings.SubscriberName : PartitionReceiver.DefaultConsumerGroupName,
                _settings.AzureEventHubConnectionString,
                _settings.AzureEventHubCheckpointConnectionString,
                // the convention of the container name is the same as the event hub name, but with dots replaced with dashes
                messageType.GetEventHubName().Replace(".","-") 
            );
            _eventProcessorHosts.Add(messageType, eventProcessorHost);
        }

        public void RegisterEvent(Type type)
        {
            if (_eventClients.ContainsKey(type))
                return;

            var topicName = GetHubNameFromType(type);
            var eventHubsConnectionStringBuilder = new EventHubsConnectionStringBuilder(_settings.AzureEventHubConnectionString)
            {
                EntityPath = topicName
            };

            var client = EventHubClient.CreateFromConnectionString(eventHubsConnectionStringBuilder.ToString());
            _eventClients.Add(type, client);
        }

        public void RegisterCommand(Type type)
        {
            if (_commandClients.ContainsKey(type))
                return;

            var queueName = GetHubNameFromType(type);
            var eventHubsConnectionStringBuilder = new EventHubsConnectionStringBuilder(_settings.AzureEventHubConnectionString)
            {
                EntityPath = queueName
            };

            var client = EventHubClient.CreateFromConnectionString(eventHubsConnectionStringBuilder.ToString());
            _commandClients.Add(type, client);
        }

        private string GetHubNameFromType(Type type)
        {
            return $"{CreatePrefix()}{type.GetEventHubName()}";
        }

        private string CreatePrefix()
        {
            if (string.IsNullOrEmpty(_settings.ServiceBusMasterPrefix))
                return "";
            return _settings.ServiceBusMasterPrefix.ToLower() + ".";
        }

        public async Task Start()
        {
            foreach (var eventProcessorHost in _eventProcessorHosts.Values)
            {
                await eventProcessorHost.RegisterEventProcessorFactoryAsync(
                    new EventProcessorFactory(
                        _handlerActivator, 
                        _exceptionLogger,
                        _hubEventErrorBus,
                        _hubCommandErrorBus,
                        _commandHandlers, 
                        _eventHandlers)
                ).ConfigureAwait(false);
            }
        }

        public async Task Stop()
        {
            foreach (var host in _eventProcessorHosts.Values)
                await host.UnregisterEventProcessorAsync().ConfigureAwait(false);

            foreach (var client in _eventClients.Values)
                await client.CloseAsync().ConfigureAwait(false);
        }
    }
}
