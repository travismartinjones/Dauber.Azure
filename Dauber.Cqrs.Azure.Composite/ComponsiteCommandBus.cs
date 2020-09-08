using System;
using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using Dauber.Core.Contracts;
using Dauber.Cqrs.Azure.EventHub;
using Dauber.Cqrs.Azure.ServiceBus;
using SimpleCqrs.Commanding;

namespace Dauber.Cqrs.Azure.Composite
{
    public class CompositeCommandBus : IAsyncCompositeCommandBus
    {
        private readonly IAsyncHubCommandBus _hubCommandBus;
        private readonly IAsyncCommandBus _serviceCommandBus;
        private readonly IAppSettings _appSettings;

        public CompositeCommandBus(
            IAsyncHubCommandBus hubCommandBus,
            IAsyncCommandBus serviceCommandBus,
            IAppSettings appSettings)
        {
            _hubCommandBus = hubCommandBus;
            _serviceCommandBus = serviceCommandBus;
            _appSettings = appSettings;
        }

        [Obsolete("Use async method instead")]
        public int Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException("Use async signature ExecuteAsync instead.");
        }

        [Obsolete("Use async method instead")]
        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            throw new NotImplementedException("Use async signature SendAsync instead.");
        }

        public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand, IHubCommand
        {
            if (_appSettings.GetByKey("IsEventHubEnabled", false))
            {
                if (_hubCommandBus.IsCommandTypeHandled(command))
                {
                    await _hubCommandBus.SendAsync(command).ConfigureAwait(false);
                    return;
                }
            }

            await _serviceCommandBus.SendAsync(command).ConfigureAwait(false);
        }

        public async Task<int> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand, HighIronRanch.Azure.ServiceBus.Contracts.ICommand
        {
            return await _serviceCommandBus.ExecuteAsync(command).ConfigureAwait(false);
        }

        public async Task SendAsync<TCommand>(TCommand command, DateTime? enqueueTime = null) where TCommand : ICommand, HighIronRanch.Azure.ServiceBus.Contracts.ICommand
        {
            await _serviceCommandBus.SendAsync(command, enqueueTime).ConfigureAwait(false);
        }

        public bool IsCommandTypeHandled(ICommand command)
        {
            return _hubCommandBus.IsCommandTypeHandled(command) || _serviceCommandBus.IsCommandTypeHandled(command);
        }
    }
}