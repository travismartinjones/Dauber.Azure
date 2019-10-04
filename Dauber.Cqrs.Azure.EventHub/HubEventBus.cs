using System.Collections.Generic;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.EventHub
{
    public class HubEventBus : IEventBus
    {
        private readonly IEventHubService _bus;

        public HubEventBus(IEventHubService bus)
        {
            _bus = bus;
        }

        public async Task PublishEvent(DomainEvent domainEvent)
        {
            await _bus.PublishAsync((IHubEvent)domainEvent).ConfigureAwait(false);
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
            if (domainEvent is IHubEvent hubEvent)
            {
                return !hubEvent.IsHubError;
            }

            return false;
        }
    }
}