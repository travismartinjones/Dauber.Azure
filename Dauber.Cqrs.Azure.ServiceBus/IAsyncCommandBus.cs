using System.Threading.Tasks;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public interface IAsyncCommandBus : ICommandBus
    {
        Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand;
        Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
    }
}