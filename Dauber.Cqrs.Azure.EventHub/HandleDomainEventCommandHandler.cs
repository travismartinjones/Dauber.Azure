using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus.Contracts;
using SimpleCqrs.Domain;

namespace Dauber.Cqrs.Azure.EventHub
{
    public abstract class HandleDomainEventCommandHandler<T> : IAggregateCommandHandler<T> where T : HandleDomainEventCommandBase
    {
        private readonly IDomainRepository _domainRepository;

        protected HandleDomainEventCommandHandler(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public async Task HandleAsync(T message, ICommandActions actions)
        {
            await _domainRepository.ProcessEvent(message.Event).ConfigureAwait(false);
        }
    }
}