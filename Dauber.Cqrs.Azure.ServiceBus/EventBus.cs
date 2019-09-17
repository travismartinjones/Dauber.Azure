using System.Collections.Generic;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus.Contracts;

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
            await _serviceBus.PublishAsync((IEvent)domainEvent).ConfigureAwait(false);
        }

        public async Task PublishEvents(IEnumerable<SimpleCqrs.Eventing.DomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                await PublishEvent(domainEvent).ConfigureAwait(false);
            }
        }
    }
}