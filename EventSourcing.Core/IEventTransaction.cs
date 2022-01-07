namespace EventSourcing.Core;

public interface IEventTransaction<TBaseEvent> : ITransaction where TBaseEvent : Event, new()
{
  Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);
}
