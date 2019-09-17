using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public interface ICsvHandlerStatusProcessor : IHandlerStatusProcessor
    {
        string ToCsv();
    }
}