using System.Collections.Generic;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus;

namespace Cqrs.Azure.ServiceBus
{
    public class EventBus : SimpleCqrs.Eventing.IEventBus
    {
        private readonly IServiceBusWithHandlers _serviceBus;

        public EventBus(IServiceBusWithHandlers serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public async Task PublishEvent(SimpleCqrs.Eventing.DomainEvent domainEvent)
        {
            await _serviceBus.PublishAsync((HighIronRanch.Azure.ServiceBus.Contracts.IEvent)domainEvent);
        }

        public async Task PublishEvents(IEnumerable<SimpleCqrs.Eventing.DomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                await PublishEvent(domainEvent);
            }
        }
    }
}