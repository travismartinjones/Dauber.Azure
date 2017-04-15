using System.Collections.Generic;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public interface IServiceBusTelemetryProperties
    {
        Dictionary<string, string> Properties { get; }
        void Add(string key, string value);
    }
}