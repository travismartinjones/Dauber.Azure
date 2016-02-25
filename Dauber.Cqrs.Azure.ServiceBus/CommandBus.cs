using System;
using HighIronRanch.Azure.ServiceBus;

namespace Cqrs.Azure.ServiceBus
{
    public class CommandBus : SimpleCqrs.Commanding.ICommandBus
    {
        private readonly IServiceBusWithHandlers _serviceBus;

        public CommandBus(IServiceBusWithHandlers serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public int Execute<TCommand>(TCommand command) where TCommand : SimpleCqrs.Commanding.ICommand
        {
            throw new NotImplementedException("Haven't gotten to this yet.");
        }

        public void Send<TCommand>(TCommand command) where TCommand : SimpleCqrs.Commanding.ICommand
        {
            var task = _serviceBus.SendAsync((HighIronRanch.Azure.ServiceBus.Contracts.ICommand)command);
            task.Wait();
        }
    }
}
