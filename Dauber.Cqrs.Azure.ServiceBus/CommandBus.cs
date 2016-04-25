using System;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class CommandBus : IAsyncCommandBus
    {
        private readonly IServiceBusWithHandlers _serviceBus;

        public CommandBus(IServiceBusWithHandlers serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public int Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException();
        }

        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            SendAsync(command).Wait();
        }
        
        public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            await _serviceBus.SendAsync((HighIronRanch.Azure.ServiceBus.Contracts.ICommand)command);
        }
    }
}
