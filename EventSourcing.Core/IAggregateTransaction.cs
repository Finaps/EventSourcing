namespace EventSourcing.Core;

public interface IAggregateTransaction<TBaseEvent> : ITransaction where TBaseEvent : Event, new()
{
  Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();
}