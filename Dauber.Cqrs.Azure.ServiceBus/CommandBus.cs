using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dauber.Core.Container;
using HighIronRanch.Azure.ServiceBus;
using SimpleCqrs.Commanding;
using FluentValidation;
using FluentValidation.Results;

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
        
        public async Task SendAsync<TCommand>(TCommand command, DateTime? enqueueTime = null) where TCommand : ICommand
        {
            var validators = IoC.GetAllInstances<IValidator<TCommand>>();

            var errors = new List<ValidationFailure>();

            foreach(var validator in validators)
            { 
                var result = validator.Validate(command);

                if (!result.IsValid)
                    errors.AddRange(result.Errors);
            }

            if(errors.Count > 0)
                throw new ValidationException(errors);

            await _serviceBus.SendAsync((HighIronRanch.Azure.ServiceBus.Contracts.ICommand)command, enqueueTime);
        }
    }
}
