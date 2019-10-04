using System.Threading.Tasks;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IHubEventHandler<in T> where T : IHubEvent
    {
        Task HandleAsync(T evt);
    }
}