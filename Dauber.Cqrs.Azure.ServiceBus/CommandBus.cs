using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dauber.Core.Container;
using HighIronRanch.Azure.ServiceBus;
using FluentValidation;
using FluentValidation.Results;
using HighIronRanch.Azure.ServiceBus.Contracts;
using ICommand = SimpleCqrs.Commanding.ICommand;

namespace Dauber.Cqrs.Azure.ServiceBus
{    
    public class ExecuteCommandActions : ICommandActions
    {
        public async Task RenewLockAsync()
        {            
        }
    }

    public class CommandBus : IAsyncCommandBus
    {
        private readonly IServiceBusWithHandlers _serviceBus;

        public CommandBus(            
            IServiceBusWithHandlers serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public int Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException("Use async signature ExecuteAsync instead.");
        }

        public async Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand, HighIronRanch.Azure.ServiceBus.Contracts.ICommand
        {
            ValidateCommand(command);

            // TODO: Consider implementing relays to allow command handlers to exist outside of the app boundary
            var handler = SimpleCqrs.ServiceLocator.Current.Resolve<ICommandHandler<TCommand>>();

            if(handler == null)
                throw new NotSupportedException($"Command {typeof(Command)} is not implemented in this application boundary. External service handlers is not supported. Use SendAsync<T> instead.");

            await handler.HandleAsync(command, new ExecuteCommandActions()).ConfigureAwait(false);
            return 1;
        }        

        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            SendAsync(command).Wait();
        }
        
        public async Task SendAsync<TCommand>(TCommand command, DateTime? enqueueTime = null) where TCommand : ICommand
        {            
            ValidateCommand(command);

            await _serviceBus.SendAsync((HighIronRanch.Azure.ServiceBus.Contracts.ICommand)command, enqueueTime).ConfigureAwait(false);
        }

        private static void ValidateCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            var validators = IoC.GetAllInstances<IValidator<TCommand>>();

            var errors = new List<ValidationFailure>();

            foreach (var validator in validators)
            {
                var result = validator.Validate(command);

                if (!result.IsValid)
                    errors.AddRange(result.Errors);
            }

            if (errors.Count > 0)
                throw new ValidationException(errors);
        }
    }
}
