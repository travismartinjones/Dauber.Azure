namespace Dauber.Azure.EventHub.Contracts
{
    public interface IAggregateHubEventHandler<T> : IHubEventHandler<T> where T : IAggregateHubEvent
    {		
    }
}