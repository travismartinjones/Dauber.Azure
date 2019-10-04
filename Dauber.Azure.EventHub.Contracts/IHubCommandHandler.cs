using System.Threading.Tasks;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IHubCommandHandler<in T> where T : IHubCommand
    {
        Task HandleAsync(T message);
    }
}