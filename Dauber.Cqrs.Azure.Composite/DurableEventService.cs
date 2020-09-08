using System;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Cqrs.Azure.EventHub;
using Dauber.Cqrs.Azure.ServiceBus;
using SimpleCqrs.Domain;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.Composite
{
    public class DurableEventService : IDurableEventService
    {
        private readonly IDurableDomainEventConverter _durableDomainEventConverter;
        private readonly IAsyncCompositeCommandBus _compositeCommandBus;
        private readonly IAsyncCommandBus _commandBus;

        public DurableEventService(
            IDurableDomainEventConverter durableDomainEventConverter,
            IAsyncCompositeCommandBus compositeCommandBus,
            IAsyncCommandBus commandBus)
        {
            _durableDomainEventConverter = durableDomainEventConverter ?? throw new ArgumentNullException(nameof(durableDomainEventConverter), "First implement an IDurableDomainEventConverter that converts to a EventHubEvent.");
            _compositeCommandBus = compositeCommandBus;
            _commandBus = commandBus;
        }

        public async Task Deliver(DomainEvent evt)
        {
            var command = _durableDomainEventConverter.Convert(evt);

            if (command is IHubCommand)
                await _compositeCommandBus.SendAsync((HubCommand) command).ConfigureAwait(false);
            else
                await _commandBus.SendAsync(command).ConfigureAwait(false);
        }
    }
}