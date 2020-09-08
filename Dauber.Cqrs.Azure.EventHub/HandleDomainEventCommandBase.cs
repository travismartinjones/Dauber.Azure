using System;
using System.Runtime.Serialization;
using Dauber.Cqrs.Azure.ServiceBus;
using Newtonsoft.Json;
using SimpleCqrs.Eventing;

namespace Dauber.Cqrs.Azure.EventHub
{
    public abstract class HandleDomainEventCommandBase : HubCommand
    {
        protected HandleDomainEventCommandBase() {}

        protected HandleDomainEventCommandBase(DomainEvent evt)
        {
            AggregateRootId = evt.AggregateRootId;
            CorrelationId = evt.SaveCorrelationId;
            Event = evt;
        }

        [JsonProperty]
        private string EventType { get; set; }
        [JsonProperty]
        private string EventJson { get; set; }

        [JsonIgnore] public DomainEvent _event;
        [JsonIgnore]
        public DomainEvent Event {
            get
            {
                if (_event == null)
                {
                    var type = Type.GetType(EventType);
                    _event = JsonConvert.DeserializeObject(EventJson, type) as DomainEvent;
                }

                return _event;
            }
            set
            {
                _event = value;
                EventType = value.GetType().AssemblyQualifiedName;
                EventJson = JsonConvert.SerializeObject(value);
            }
        }
    }
}