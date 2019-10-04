namespace Dauber.Azure.EventHub.Contracts
{
    public interface IAggregateHubEvent : IHubEvent
    {
        string GetAggregateId();
    }
}