using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.Composite
{
    public class CompositeEventBus : IEventBus
    {
        private readonly IEnumerable<IEventBus> _buses;

        public CompositeEventBus(IEnumerable<IEventBus> buses)
        {
            _buses = buses;
        }

        public async Task PublishEvent(DomainEvent domainEvent)
        {
            foreach (var bus in _buses)
            {
                if (!bus.IsEventTypeHandled(domainEvent)) continue;
                await bus.PublishEvent(domainEvent).ConfigureAwait(false);
            }
        }

        public async Task PublishEvents(IEnumerable<DomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                await PublishEvent(domainEvent).ConfigureAwait(false);
            }
        }

        public bool IsEventTypeHandled(DomainEvent domainEvent)
        {
            return _buses.Any(x => x.IsEventTypeHandled(domainEvent));
        }
    }
}
