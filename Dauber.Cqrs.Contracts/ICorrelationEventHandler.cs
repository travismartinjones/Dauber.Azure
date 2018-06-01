using System.Threading.Tasks;

namespace Dauber.Cqrs.Contracts
{
    public interface ICorrelationEventHandler
    {
        Task Handle(ICorrelationEvent evt);
    }
}