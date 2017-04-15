using System.Collections.Generic;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class ServiceBusTelemetryProperties : IServiceBusTelemetryProperties
    {
        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
        public Dictionary<string, string> Properties => _properties;

        public void Add(string key, string value)
        {
            _properties[key] = value;
        }
    }
}