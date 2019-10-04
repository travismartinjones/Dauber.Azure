using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using HighIronRanch.Azure.ServiceBus;

namespace Dauber.Cqrs.Azure.EventHub
{
    public class HubCommandErrorBus : IHubCommandErrorBus
    {
        private readonly IServiceBusWithHandlers _bus;

        public HubCommandErrorBus(IServiceBusWithHandlers bus)
        {
            _bus = bus;
        }

        public async Task ProcessAsync<T>(T command) where T : IHubCommand
        {
            await _bus.SendAsync(command).ConfigureAwait(false);
        }
    }
}