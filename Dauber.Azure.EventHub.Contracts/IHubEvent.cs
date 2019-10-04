using System;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IHubEvent : IEvent
    {
        bool IsHubError { get; set; }
    }
}