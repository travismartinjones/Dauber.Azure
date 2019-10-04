using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using HighIronRanch.Azure.ServiceBus;

namespace Dauber.Cqrs.Azure.EventHub
{
    public class HubEventErrorBus : IHubEventErrorBus
    {
        private readonly IServiceBusWithHandlers _bus;

        public HubEventErrorBus(IServiceBusWithHandlers bus)
        {
            _bus = bus;
        }

        public async Task ProcessAsync<T>(T evt) where T : IHubEvent
        {
            await _bus.PublishAsync(evt).ConfigureAwait(false);
        }
    }
}