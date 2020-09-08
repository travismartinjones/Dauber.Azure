using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Cqrs.Azure.ServiceBus;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.Composite
{
    public interface IAsyncCompositeCommandBus : IAsyncCommandBus
    {
        // this method uses either even hub or service bus depending on command attributes
        Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand, IHubCommand;
    }
}