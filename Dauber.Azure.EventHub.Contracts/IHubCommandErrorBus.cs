using System.Threading.Tasks;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IHubCommandErrorBus
    {
        Task ProcessAsync<T>(T evt) where T : IHubCommand;
    }
}