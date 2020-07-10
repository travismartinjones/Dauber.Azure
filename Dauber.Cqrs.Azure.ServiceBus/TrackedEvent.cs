using System;
using Dauber.Cqrs.Contracts;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class TrackedEvent : Event
    {
        public Guid UserId { get; set; } 
        public string UsersName { get; set; }
    }
}