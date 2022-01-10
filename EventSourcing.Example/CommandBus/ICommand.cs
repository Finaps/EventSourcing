using System;

namespace EventSourcing.Example.CommandBus;

public interface ICommand
{
    Guid AggregateId { get; }
}