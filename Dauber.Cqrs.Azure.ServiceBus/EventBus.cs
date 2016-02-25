using System.Collections.Generic;
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

        public void PublishEvent(SimpleCqrs.Eventing.DomainEvent domainEvent)
        {
            var task = _serviceBus.PublishAsync((HighIronRanch.Azure.ServiceBus.Contracts.IEvent)domainEvent);
            task.Wait();
        }

        public void PublishEvents(IEnumerable<SimpleCqrs.Eventing.DomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                PublishEvent(domainEvent);
            }
        }
    }
}