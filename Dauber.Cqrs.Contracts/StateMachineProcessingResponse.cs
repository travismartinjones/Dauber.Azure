namespace Dauber.Cqrs.Contracts
{
    public class StateMachineProcessingResults
    {
        public bool IsEventHandled { get; set; }
        public bool IsSagaComplete { get; set; }
    }
}