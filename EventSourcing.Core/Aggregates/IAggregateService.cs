namespace EventSourcing.Core;

public interface IAggregateService : IAggregateService<Event> { }

/// <summary>
/// Aggregate Service Interface: Rehydrating and Persisting <see cref="Aggregate"/>s from <see cref="Event"/>s
/// </summary>
/// <typeparam name="TBaseEvent">Base <see cref="Event"/> for <see cref="IAggregateService{TBaseEvent}"/></typeparam>
public interface IAggregateService<TBaseEvent> where TBaseEvent : Event, new()
{
  /// <summary>
  /// Rehydrate <see cref="Aggregate"/>
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> or null when not found</returns>
  Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();
  
  /// <summary>
  /// Rehydrate <see cref="Aggregate"/> up to a certain date
  /// </summary>
  /// <param name="aggregateId">Unique partition identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="date">Date up to which to rehydrate <see cref="Aggregate"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> as it was on <c>date</c> or null when not found</returns>
  Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();
  
  /// <summary>
  /// Persist <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate"><see cref="Aggregate"/> to persist</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns>Persisted <see cref="Aggregate"/></returns>
  Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();
  
  /// <summary>
  /// Persist multiple <see cref="Aggregate"/> in a transaction
  /// </summary>
  /// <param name="aggregates"><see cref="Aggregate"/> to persist</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  Task PersistAsync(IEnumerable<Aggregate<TBaseEvent>> aggregates,
    CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Create Aggregate Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  IAggregateTransaction<TBaseEvent> CreateTransaction(Guid partitionId);
  
  /// <summary>
  /// Create Aggregate Transaction for <see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  IAggregateTransaction<TBaseEvent> CreateTransaction();
  
  /// <summary>
  /// Rehydrate <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> or null when not found</returns>
  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new() =>
    await RehydrateAsync<TAggregate>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Rehydrate <see cref="Aggregate"/> up to a certain date
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="date">Date up to which to rehydrate <see cref="Aggregate"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> as it was on <c>date</c> or null when not found</returns>
  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new() =>
    await RehydrateAsync<TAggregate>(Guid.Empty, aggregateId, date, cancellationToken);
  
  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate"/>
  /// </summary>
  /// <param name="partitionId">Unique partition identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="action"><c>Action</c> to take before Persisting</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid partitionId, Guid aggregateId, Action<TAggregate> action,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    var aggregate = await RehydrateAsync<TAggregate>(partitionId, aggregateId, cancellationToken);
    action.Invoke(aggregate);
    return await PersistAsync(aggregate, cancellationToken);
  }

  /// <summary>
  /// Rehydrate and Persist <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
  /// <param name="action"><c>Action</c> to take before Persisting</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> or null when not found</returns>
  public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, Action<TAggregate> action,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    var aggregate = await RehydrateAsync<TAggregate>(aggregateId, cancellationToken);
    action.Invoke(aggregate);
    return await PersistAsync(aggregate, cancellationToken);
  }
}