namespace Dauber.Azure.EventHub.Contracts
{
    public interface IAggregateHubCommandHandler<T> : IHubCommandHandler<T> where T : IAggregateHubCommand
    {
    }
}