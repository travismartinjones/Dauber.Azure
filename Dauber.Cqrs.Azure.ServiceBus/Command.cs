using System;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class Command : CommandWithAggregateRootId, IAggregateCommand
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string GetAggregateId()
        {
            return AggregateRootId.ToString();
        }
    }
}