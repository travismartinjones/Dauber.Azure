namespace Dauber.Azure.EventHub.Contracts
{
    public interface IEventHubSettings
    {
        string AzureEventHubConnectionString { get; }
        string AzureEventHubCheckpointConnectionString { get; }
        string ServiceBusMasterPrefix { get; }
        // eg. s.site
        string SubscriberName { get; }
    }
}