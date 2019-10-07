using System;

namespace Dauber.Azure.EventHub
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventHubAttribute : Attribute
    {
        public string Name { get; }
        public bool IsFallbackToServiceBusEnabled { get; }

        public EventHubAttribute(string name, bool isFallbackToServiceBusEnabled)
        {
            Name = name;
            IsFallbackToServiceBusEnabled = isFallbackToServiceBusEnabled;
        }
    }
}