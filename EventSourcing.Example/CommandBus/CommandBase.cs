using System;

namespace EventSourcing.Example.CommandBus
{
    public record CommandBase(Guid AggregateId) : ICommand;
}