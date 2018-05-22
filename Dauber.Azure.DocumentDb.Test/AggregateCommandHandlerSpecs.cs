using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dauber.Core.Exceptions;
using Dauber.Core.Time;
using Dauber.Cqrs.Azure.ServiceBus;
using FakeItEasy;
using HighIronRanch.Azure.ServiceBus;
using HighIronRanch.Azure.ServiceBus.Contracts;
using Xunit;

namespace Dauber.Azure.DocumentDb.Test
{    
    internal class CommandWithUserId : Command
    {
        public Guid UserId { get; set; }
    }
    
    internal class CommandWithoutUserId : Command
    {        
    }

    [AggregateCommandRejected]
    internal class AggregateCommandRejectedException : Exception
    {

    }

    internal class CommandErrorEvent : Event, ICommandErrorEvent
    {
        public Guid Id { get; set; }
        public Guid CommandMessageId { get; set; }
        public string CommandName { get; set; }
        public List<string> Errors { get; set; }
        public Guid? UserId { get; set; }
    }

    internal class CommandWithUserIdHandler : AggregateCommandHandler<CommandWithUserId, CommandErrorEvent>
    {
        public CommandWithUserIdHandler(IServiceBusTelemetryProperties serviceBusTelemetryProperties, IServiceBusWithHandlers bus, IExceptionLogger exceptionLogger, IDateTime dateTime) : base(serviceBusTelemetryProperties, bus, exceptionLogger, dateTime)
        {
        }

        public override Task ProcessAsync(CommandWithUserId message, ICommandActions actions)
        {
            throw new AggregateCommandRejectedException();
        }
    }

    internal class CommandWithoutUserIdHandler : AggregateCommandHandler<CommandWithoutUserId, CommandErrorEvent>
    {
        public CommandWithoutUserIdHandler(IServiceBusTelemetryProperties serviceBusTelemetryProperties, IServiceBusWithHandlers bus, IExceptionLogger exceptionLogger, IDateTime dateTime) : base(serviceBusTelemetryProperties, bus, exceptionLogger, dateTime)
        {
        }

        public override Task ProcessAsync(CommandWithoutUserId message, ICommandActions actions)
        {
            throw new AggregateCommandRejectedException();
        }
    }

    public class AggregateCommandHandlerSpecs
    {
        [Fact]
        public async Task commands_with_a_userid_property_is_included_in_command_error_events()
        {
            var serviceBusTelemetryProperties = A.Fake<IServiceBusTelemetryProperties>();
            var serviceBusWithHandlers = A.Fake<IServiceBusWithHandlers>();
            var exceptionLogger = A.Fake<IExceptionLogger>();
            var dateTime = A.Fake<IDateTime>();
            var commandActions = A.Fake<ICommandActions>();

            var commandWithUserIdHandler = new CommandWithUserIdHandler(
                serviceBusTelemetryProperties,
                serviceBusWithHandlers,
                exceptionLogger,
                dateTime);

            var id = new Guid("15515e68-a760-4925-8398-6181397d4940");
            await commandWithUserIdHandler.HandleAsync(new CommandWithUserId {UserId = id}, commandActions);

            A.CallTo(() => serviceBusWithHandlers.PublishAsync(A<IEvent>.That.Matches(x => ((CommandErrorEvent)x).UserId == id)))
                .MustHaveHappened();            
        }
        
        [Fact]
        public async Task commands_with_a_userid_property_is_not_included_in_command_error_events()
        {
            var serviceBusTelemetryProperties = A.Fake<IServiceBusTelemetryProperties>();
            var serviceBusWithHandlers = A.Fake<IServiceBusWithHandlers>();
            var exceptionLogger = A.Fake<IExceptionLogger>();
            var dateTime = A.Fake<IDateTime>();
            var commandActions = A.Fake<ICommandActions>();

            var commandWithUserIdHandler = new CommandWithoutUserIdHandler(
                serviceBusTelemetryProperties,
                serviceBusWithHandlers,
                exceptionLogger,
                dateTime);
            
            await commandWithUserIdHandler.HandleAsync(new CommandWithoutUserId(), commandActions);

            A.CallTo(() => serviceBusWithHandlers.PublishAsync(A<IEvent>.That.Matches(x => !((CommandErrorEvent)x).UserId.HasValue)))
                .MustHaveHappened();            
        }
    }
}