using System;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Contracts
{
    public interface ICorrelationEvent : IAggregateEvent
    {
        Guid? CorrelationId { get; set; }
    }
}