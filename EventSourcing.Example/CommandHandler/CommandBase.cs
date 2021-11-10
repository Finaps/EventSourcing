using System;

namespace EventSourcing.Example.CommandHandler
{
    public record CommandBase(Guid AggregateId) : ICommand;
}