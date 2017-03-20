using System;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AggregateCommandRejectedAttribute : Attribute
    {
        
    }
}