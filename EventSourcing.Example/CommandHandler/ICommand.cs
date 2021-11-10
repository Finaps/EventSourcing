using System;

namespace EventSourcing.Example.Commands
{
    public interface ICommand
    {
        Guid AggregateId { get; }
    }
}