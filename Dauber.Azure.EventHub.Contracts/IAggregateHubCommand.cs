using System;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IAggregateHubCommand : IHubCommand
    {
        Guid MessageId { get; }
        string GetAggregateId();
    }
}