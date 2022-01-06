namespace EventSourcing.Core;

public interface ISnapshotStore : ISnapshotStore<Event> { }
    
public interface ISnapshotStore<TBaseEvent> where TBaseEvent : Event
{
    /// <summary>
    /// Queryable and AsyncEnumerable Collection of <see cref="Snapshots"/>s
    /// </summary>
    /// <remarks>
    /// Implementations of this method should implement the <c cref="IQueryable">IQueryable</c> and
    /// <c cref="IAsyncEnumerable{T}">IAsyncEnumerable</c> interfaces, such that the async extensions
    /// e.g. <c>System.Linq.Async</c> or <c>EventSourcing.Core.QueryableExtensions</c> work as intended.
    /// </remarks>
    IQueryable<TBaseEvent> Snapshots { get; }
        
    /// <summary>
    /// Add snapshot as <see cref="Event"/> to the <see cref="ISnapshotStore{TBaseEvent}"/>
    /// </summary>
    /// <param name="snapshot">Snapshot to add</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    Task AddSnapshotAsync(TBaseEvent snapshot, CancellationToken cancellationToken = default);
}