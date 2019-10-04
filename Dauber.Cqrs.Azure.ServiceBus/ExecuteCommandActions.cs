using System.Threading;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class ExecuteCommandActions : ICommandActions
    {
        public async Task RenewLockAsync()
        {            
        }

        public int DeliveryCount => 1;
        public bool IsLastDelivery => true;
        public CancellationToken CancellationToken => new CancellationToken();
    }
}