using System;

namespace Dauber.Azure.EventHub
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventHubAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Name { get; }
        public bool IsFallbackToServiceBusEnabled { get; }

        public EventHubAttribute(string @namespace, string name, bool isFallbackToServiceBusEnabled)
        {
            Namespace = @namespace;
            Name = name;
            IsFallbackToServiceBusEnabled = isFallbackToServiceBusEnabled;
        }
    }
}