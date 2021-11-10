using System;

namespace EventSourcing.Example.CommandHandler
{
    public interface ICommand
    {
        Guid AggregateId { get; }
    }
}