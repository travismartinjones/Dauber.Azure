using System;

namespace Dauber.Azure.EventHub
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventHubAttribute : Attribute
    {
        public string Name { get; }

        public EventHubAttribute(string name)
        {
            Name = name;
        }
    }
}