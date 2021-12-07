using System;

namespace EventSourcing.Example.ComandBus
{
    public record CommandBase(Guid AggregateId) : ICommand;
}