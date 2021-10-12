using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IEventStore : IEventStore<Event> { }

  public interface IEventStore<TBaseEvent> where TBaseEvent : Event
  {
    IQueryable<TBaseEvent> Events { get; }
    Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);
  }
}