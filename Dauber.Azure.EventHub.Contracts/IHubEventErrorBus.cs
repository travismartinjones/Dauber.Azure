using System.Threading.Tasks;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IHubEventErrorBus
    {
        Task ProcessAsync<T>(T evt) where T : IHubEvent;
    }
}