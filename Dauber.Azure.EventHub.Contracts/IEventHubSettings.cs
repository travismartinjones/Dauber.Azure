using System.Collections.Generic;

namespace Dauber.Azure.EventHub.Contracts
{
    public class EventHubConnectionString
    {
        public string Namespace { get; set; }
        public string ConnectionString { get; set; }
    }

    public interface IEventHubSettings
    {
        IEnumerable<EventHubConnectionString> AzureEventHubConnectionStrings { get; }
        string AzureEventHubCheckpointConnectionString { get; }
        string ServiceBusMasterPrefix { get; }
        // eg. s.site
        string SubscriberName { get; }
        bool IsFallbackToServiceBusEnabled { get; }
    }
}