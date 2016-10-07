using System;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class Event : DomainEvent, IEvent
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
    }
}