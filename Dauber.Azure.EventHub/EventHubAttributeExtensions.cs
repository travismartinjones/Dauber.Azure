using System;

namespace Dauber.Azure.EventHub
{
    public static class EventHubAttributeExtensions
    {
        public static string GetEventHubName(this Type type)
        {
            var attribute = ((EventHubAttribute) Attribute.GetCustomAttribute(type, typeof(EventHubAttribute)));
            if(attribute == null)
                throw new Exception("Attribute EventHub is required on all HubEvent and HubCommand types.");
            return attribute.Name;
        }
        
        public static string GetEventHubConnectionKey(this Type type)
        {
            var attribute = ((EventHubAttribute) Attribute.GetCustomAttribute(type, typeof(EventHubAttribute)));
            if(attribute == null)
                throw new Exception("Attribute EventHub is required on all HubEvent and HubCommand types.");
            return attribute.Namespace;
        }
    }
}