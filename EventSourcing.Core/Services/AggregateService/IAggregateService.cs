namespace Finaps.EventSourcing.Core;

/// <summary>
/// Allows Rehydrating and Persisting <see cref="Aggregate{TAggregate}"/>s from <see cref="Event"/> streams
/// </summary>
public interface IAggregateService
{
  /// <summary>
  /// Rehydrate <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> or <c>null</c> when not found</returns>
  Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new();
  
  /// <summary>
  /// Rehydrate <see cref="Aggregate{TAggregate}"/> up to a certain date.
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="date">Date up to which to rehydrate <see cref="Aggregate{TAggregate}"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> as it was on <c>date</c> or null when not found</returns>
  Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <remarks>
  /// This will store all uncommitted <see cref="Event"/>s of an <see cref="Aggregate{TAggregate}"/>.
  /// </remarks>
  /// <param name="aggregate"><see cref="Aggregate{TAggregate}"/> to persist</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns>Persisted <see cref="Aggregate{TAggregate}"/></returns>
  Task PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Create ACID Aggregate Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns><see cref="IAggregateTransaction"/></returns>
  IAggregateTransaction CreateTransaction(Guid partitionId);
  
  /// <summary>
  /// Create ACID Aggregate Transaction for default (<see cref="Guid"/>.<see cref="Guid.Empty"/>) PartitionId
  /// </summary>
  /// <returns><see cref="IAggregateTransaction"/></returns>
  IAggregateTransaction CreateTransaction();

  /// <summary>
  /// Rehydrate <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> or null when not found</returns>
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await RehydrateAsync<TAggregate>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Rehydrate <see cref="Aggregate{TAggregate}"/> up to a certain date
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="date">Date up to which to rehydrate <see cref="Aggregate{TAggregate}"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> as it was on <c>date</c> or null when not found</returns>
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await RehydrateAsync<TAggregate>(Guid.Empty, aggregateId, date, cancellationToken);

  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="action"><c>Action</c> to take before Persisting</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid partitionId, Guid aggregateId, Action<TAggregate> action,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new()
  {
    var aggregate = await RehydrateAsync<TAggregate>(partitionId, aggregateId, cancellationToken);

    if (aggregate == null)
      throw new ArgumentException($"Cannot Rehydrate and Persist Aggregate with PartitionId '{partitionId}' and Id '{aggregateId}': Aggregate does not exist.");
    
    action.Invoke(aggregate);
    await PersistAsync(aggregate, cancellationToken);
    return aggregate;
  }

  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="action"><c>Action</c> to take before Persisting</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, Action<TAggregate> action,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await RehydrateAndPersistAsync(Guid.Empty, aggregateId, action, cancellationToken);
  
  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await RehydrateAndPersistAsync<TAggregate>(partitionId, aggregateId, _ => {}, cancellationToken);
  
  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <c>TAggregate</c> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await RehydrateAndPersistAsync<TAggregate>(Guid.Empty, aggregateId, _ => {}, cancellationToken);
}