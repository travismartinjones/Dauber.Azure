using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Container;
using FluentValidation;
using FluentValidation.Results;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.EventHub
{
    public class HubCommandBus : IAsyncHubCommandBus
    {
        private readonly IEventHubService _hubService;

        public HubCommandBus(            
            IEventHubService hubService)
        {
            _hubService = hubService;
        }

        [Obsolete("Use async method instead")]
        public int Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException("Use async signature ExecuteAsync instead.");
        }

        public async Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand, IHubCommand
        {
            ValidateCommand(command);

            // TODO: Consider implementing relays to allow command handlers to exist outside of the app boundary
            var handler = SimpleCqrs.ServiceLocator.Current.Resolve<IHubCommandHandler<TCommand>>();

            if(handler == null)
                throw new NotSupportedException($"Command {typeof(HubCommand)} is not implemented in this application boundary. External service handlers is not supported. Use SendAsync<T> instead.");

            await handler.HandleAsync(command).ConfigureAwait(false);
            return 1;
        }

        public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand, IHubCommand
        {
            await _hubService.SendAsync(command).ConfigureAwait(false);
        }

        [Obsolete("Use async method instead")]
        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException("Use async signature SendAsync instead.");
        }

        public bool IsCommandTypeHandled(ICommand command)
        {
            return command is IHubCommand;
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