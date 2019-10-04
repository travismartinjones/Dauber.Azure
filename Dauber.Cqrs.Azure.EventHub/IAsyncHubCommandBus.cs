using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.EventHub
{
    public interface IAsyncHubCommandBus : ICommandBus
    {
        Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand, IHubCommand;
        Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand, IHubCommand;
    }
}