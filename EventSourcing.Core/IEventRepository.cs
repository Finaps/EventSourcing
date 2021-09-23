using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IEventRepository<out TEvent> where TEvent : Event
  {
    IQueryable<TEvent> Events { get; }
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new();
    Task PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new();
  }
}