using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IEventStore : IEventStore<Event> { }

  public interface IEventStore<out TEvent> where TEvent : Event
  {
    IAsyncEnumerable<T> Query<T>(Func<IQueryable<TEvent>, IQueryable<T>> func);
    Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default);
  }
}