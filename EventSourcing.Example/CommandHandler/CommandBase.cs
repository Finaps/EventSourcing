using System;

namespace EventSourcing.Example.Commands
{
    public record CommandBase(Guid AggregateId) : ICommand;
}