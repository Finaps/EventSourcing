using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core
{
  public interface IEventStore : IEventStore<Event> { }

  /// <summary>
  /// Event Store Interface: Persisting <see cref="Events"/>s to a Database
  /// </summary>
  /// <remarks>
  /// The <c>TBaseEvent</c> type parameter determines which <see cref="Events"/> fields are queryable on a database Level
  /// </remarks>
  /// <typeparam name="TBaseEvent">Base <see cref="Events"/> for <see cref="IEventStore{TBaseEvent}"/></typeparam>
  public interface IEventStore<TBaseEvent> where TBaseEvent : Event
  {
    /// <summary>
    /// Queryable and AsyncEnumerable Collection of <see cref="Events"/>s
    /// </summary>
    /// <remarks>
    /// Implementations of this method should implement the <c cref="IQueryable{T}">IQueryable</c> and
    /// <c cref="IAsyncEnumerable{T}">IAsyncEnumerable</c> interfaces, such that the async extensions
    /// e.g. <c>System.Linq.Async</c> or <c>EventSourcing.Core.QueryableExtensions</c> work as intended.
    /// </remarks>
    IQueryable<TBaseEvent> Events { get; }
    
    /// <summary>
    /// Add <see cref="Events"/>s to the <see cref="IEventStore{TBaseEvent}"/>
    /// </summary>
    /// <remarks>
    /// When adding events an <see cref="EventStoreException"/> will occur when
    /// <list type="bullet">
    /// <item>An <see cref="Events"/> with the same <see cref="Event.EventId"/> already exists</item>
    /// <item>An <see cref="Events"/> with the same combination of <see cref="Event.AggregateId"/> and <see cref="Event.AggregateVersion"/> already exists</item>
    /// <item>The added <see cref="Events"/>s do not all share the same <see cref="Event.AggregateId"/></item>
    /// </list>
    /// </remarks>
    /// <param name="events"><see cref="Events"/>s to add</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);
  }
}