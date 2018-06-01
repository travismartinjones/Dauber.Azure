using System;
using Dauber.Cqrs.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class Event : DomainEvent, ICorrelationEvent
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid? CorrelationId { get; set; }
        public string GetAggregateId()
        {
            return AggregateRootId.ToString();
        }
    }
}