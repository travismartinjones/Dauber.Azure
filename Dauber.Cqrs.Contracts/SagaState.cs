using System;

namespace Dauber.Cqrs.Contracts
{
    public class SagaState
    {
        public Type StateType { get; set; }
        public object State { get; set; }
    }
}