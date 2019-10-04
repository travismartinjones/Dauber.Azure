using System;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core;
using Dauber.Cqrs.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Commanding;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.EventHub
{
    public class HubCommand : Command, IAggregateHubCommand
    {
    }
}