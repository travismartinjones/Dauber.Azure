using Dauber.Cqrs.Azure.ServiceBus;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.EventHub
{
    public interface IDurableDomainEventConverter
    {
        Command Convert(DomainEvent evt);
    }
}