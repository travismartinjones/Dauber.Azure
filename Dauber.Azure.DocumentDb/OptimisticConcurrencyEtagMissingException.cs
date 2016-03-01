using System;

namespace Dauber.Azure.DocumentDb
{
    public class OptimisticConcurrencyEtagMissingException : Exception
    {
        public OptimisticConcurrencyEtagMissingException(string message) : base(message) {}
    }
}