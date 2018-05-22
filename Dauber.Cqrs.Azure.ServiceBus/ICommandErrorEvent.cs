using System;
using System.Collections.Generic;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public interface ICommandErrorEvent : IEvent
    {
        Guid Id { get; set; }        
        DateTime EventDate { get; set; }
        Guid CommandMessageId { get; set; }
        string CommandName { get; set; }
        List<string> Errors { get; set; }
        Guid? UserId { get; set; }
    }
}