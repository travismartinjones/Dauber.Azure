using System;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Cqrs.Azure.ServiceBus;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.EventHub
{
    public class HubEvent : Event, IAggregateHubEvent
    {
        public bool IsHubError { get; set; }
    }
}
