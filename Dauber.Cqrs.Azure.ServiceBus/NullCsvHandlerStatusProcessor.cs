using System;
using HighIronRanch.Azure.ServiceBus.Contracts;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class NullCsvHandlerStatusProcessor : IHandlerStatusProcessor
    {
        public void Begin(string handlerType, string id, DateTime eventDate)
        {            
        }

        public void Complete(string handlerType, string id, double elapsedSeconds)
        {
        }

        public void BusComplete(string handlerType, string id, double elapsedSeconds)
        {
        }

        public void BusClose(string handlerType, string id, double elapsedSeconds)
        {
        }

        public void BusAbandon(string handlerType, string id, double elapsedSeconds)
        {
        }

        public void Abandon(string handlerType, string id, Exception ex)
        {
        }

        public void Error(string handlerType, string id, Exception ex)
        {
        }

        public void Clear()
        {
        }
    }
}