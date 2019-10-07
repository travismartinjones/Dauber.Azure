using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.Composite
{
    public class CompositeEventBus : IEventBus
    {
        private readonly IEventBus _hubEventBus;
        private readonly IEventBus _serviceBusEventBus;
        private readonly IAppSettings _appSettings;
        private readonly IEnumerable<IEventBus> _buses;

        public CompositeEventBus(
            IEventBus hubEventBus,
            IEventBus serviceBusEventBus,
            IAppSettings appSettings)
        {
            _hubEventBus = hubEventBus;
            _serviceBusEventBus = serviceBusEventBus;
            _appSettings = appSettings;
        }

        public async Task PublishEvent(DomainEvent domainEvent)
        {
            if (_appSettings.GetByKey("IsEventHubEnabled", false))
            {
                if (_hubEventBus.IsEventTypeHandled(domainEvent))
                {
                    await _hubEventBus.PublishEvent(domainEvent).ConfigureAwait(false);
                    return;
                }
            }

            await _serviceBusEventBus.PublishEvent(domainEvent).ConfigureAwait(false);
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
