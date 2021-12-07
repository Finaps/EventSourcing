using System;

namespace EventSourcing.Example.ComandBus
{
    public interface ICommand
    {
        Guid AggregateId { get; }
    }
}