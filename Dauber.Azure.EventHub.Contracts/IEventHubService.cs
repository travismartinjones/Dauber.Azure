using System.Threading.Tasks;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IEventHubService
    {        
        Task SendAsync(IHubCommand command);
        Task PublishAsync(IHubEvent evt);
        Task Start();
        Task Stop();
    }
}