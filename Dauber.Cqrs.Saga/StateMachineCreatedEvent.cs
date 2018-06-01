using System;
using Dauber.Cqrs.Contracts;

namespace Dauber.Cqrs.Saga
{
    public class StateMachineCreatedEvent : ICorrelationEvent
    {
        public string GetAggregateId()
        {
            return CorrelationId.GetValueOrDefault().ToString();
        }

        public Guid? CorrelationId { get; set; }
        public Guid MessageId { get; }
    }
}