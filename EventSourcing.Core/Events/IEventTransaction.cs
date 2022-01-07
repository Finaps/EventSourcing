namespace EventSourcing.Core;

public interface IEventTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);
  Task CommitAsync(CancellationToken cancellationToken = default);
}
