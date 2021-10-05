using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IEventStore : IEventStore<Event> { }

  public interface IEventStore<out TEvent> where TEvent : Event
  {
    IQueryable<TEvent> Events { get; }
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new();
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new();
    
    Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new();
  
    async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, Action<TAggregate> action,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var aggregate = await RehydrateAsync<TAggregate>(aggregateId, cancellationToken);
      action.Invoke(aggregate);
      return await PersistAsync(aggregate, cancellationToken);
    }
    
    async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, DateTimeOffset date, Action<TAggregate> action,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var aggregate = await RehydrateAsync<TAggregate>(aggregateId, date, cancellationToken);
      action.Invoke(aggregate);
      return await PersistAsync(aggregate, cancellationToken);
    }
  }
}