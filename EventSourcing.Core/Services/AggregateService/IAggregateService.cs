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
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <see cref="TAggregate"/> or <c>null</c> when not found</returns>
  Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new();
  
  /// <summary>
  /// Rehydrate <see cref="Aggregate{TAggregate}"/> up to a certain date.
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="date">Date up to which to rehydrate <see cref="Aggregate{TAggregate}"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <see cref="TAggregate"/> as it was on <c>date</c> or null when not found</returns>
  Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new();
  
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
  Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new();
  
  /// <summary>
  /// Persist multiple <see cref="Aggregate{TAggregate}"/>s in a transaction
  /// </summary>
  /// <remarks>
  /// This will store all uncommitted <see cref="Event"/>s of all <see cref="Aggregate{TAggregate}"/>s in an ACID <see cref="IAggregateTransaction"/>.
  /// </remarks>
  /// <param name="aggregates"><see cref="Aggregate{TAggregate}"/> to persist</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task PersistAsync(IEnumerable<Aggregate> aggregates, CancellationToken cancellationToken = default);

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
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <see cref="TAggregate"/> or null when not found</returns>
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await RehydrateAsync<TAggregate>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Rehydrate <see cref="Aggregate{TAggregate}"/> up to a certain date
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="date">Date up to which to rehydrate <see cref="Aggregate{TAggregate}"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <see cref="TAggregate"/> as it was on <c>date</c> or null when not found</returns>
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await RehydrateAsync<TAggregate>(Guid.Empty, aggregateId, date, cancellationToken);

  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="action"><c>Action</c> to take before Persisting</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <see cref="TAggregate"/> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid partitionId, Guid aggregateId, Action<TAggregate> action,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    var aggregate = await RehydrateAsync<TAggregate>(partitionId, aggregateId, cancellationToken);

    if (aggregate == null)
      throw new ArgumentException($"Cannot Rehydrate and Persist Aggregate with PartitionId '{partitionId}' and Id '{aggregateId}': Aggregate does not exist.");
    
    action.Invoke(aggregate);
    return await PersistAsync(aggregate, cancellationToken);
  }

  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TAggregate}"/> to rehydrate</param>
  /// <param name="action"><c>Action</c> to take before Persisting</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TAggregate}"/></typeparam>
  /// <returns><see cref="Aggregate{TAggregate}"/> of type <see cref="TAggregate"/> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, Action<TAggregate> action,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await RehydrateAndPersistAsync(Guid.Empty, aggregateId, action, cancellationToken);
}