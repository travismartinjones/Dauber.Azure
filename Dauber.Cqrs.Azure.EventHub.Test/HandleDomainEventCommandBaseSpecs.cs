using System;
using Dauber.Core;
using NUnit.Framework;
using Shouldly;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.EventHub.Test
{
    public class HandleDomainEventCommandTest : HandleDomainEventCommandBase
    {
        public HandleDomainEventCommandTest()
        {
            
        }

        public HandleDomainEventCommandTest(DomainEvent evt) : base(evt)
        {
            
        }
    }

    public class HandleDomainEventCommandBaseSpecs
    {
        [Test]
        public void SerializingAndDeserializingShouldHaveAllProperties()
        {
            var evt = new DomainEvent
            {
                Id = Guid.NewGuid(),
                AggregateRootId = Guid.NewGuid(),
                Sequence = 1,
                EventDate = DateTime.Now
            };
            var json = new HandleDomainEventCommandTest(evt).ToJson();
            var command = json.FromJson<HandleDomainEventCommandTest>();
            command.Event.ShouldNotBeNull();
            command.AggregateRootId.ShouldNotBe(Guid.Empty);
        }
    }
}