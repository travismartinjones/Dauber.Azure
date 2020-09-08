using System;
using System.Threading.Tasks;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public interface IAsyncCommandBus : ICommandBus
    {
        Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand, HighIronRanch.Azure.ServiceBus.Contracts.ICommand;
        // this method uses either only service bus, for event hub support, use the signature without enqueueTime
        Task SendAsync<TCommand>(TCommand command, DateTime? enqueueTime = null) where TCommand : ICommand, HighIronRanch.Azure.ServiceBus.Contracts.ICommand;
    }
}