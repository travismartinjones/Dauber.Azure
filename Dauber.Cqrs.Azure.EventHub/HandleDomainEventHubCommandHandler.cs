using System.Threading.Tasks;
using Dauber.Azure.EventHub.Contracts;
using SimpleCqrs.Domain;

namespace Dauber.Cqrs.Azure.EventHub
{
    public abstract class HandleDomainEventHubCommandHandler<T> : IAggregateHubCommandHandler<T> where T : HandleDomainEventCommandBase
    {
        private readonly IDomainRepository _domainRepository;

        protected HandleDomainEventHubCommandHandler(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public async Task HandleAsync(T message)
        {
            await _domainRepository.ProcessEvent(message.Event).ConfigureAwait(false);
        }
    }
}