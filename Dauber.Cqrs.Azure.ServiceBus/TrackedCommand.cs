using System;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class TrackedCommand : Command
    {
        public Guid UserId { get; set; }
        public string UsersName { get; set; }
    }
}