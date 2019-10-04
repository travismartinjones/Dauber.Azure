using System;

namespace Dauber.Azure.EventHub.Contracts
{
    public interface IHandlerActivator
    {
        object GetInstance(Type type);
    }
}