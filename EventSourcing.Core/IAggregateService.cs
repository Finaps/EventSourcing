using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IAggregateService : IAggregateService<Event> { }

  /// <summary>
  /// Aggregate Service Interface: Rehydrating and Persisting <see cref="Aggregate"/>s from <see cref="Event"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent">Base <see cref="Event"/> for <see cref="IAggregateService{TBaseEvent}"/></typeparam>
  public interface IAggregateService<TBaseEvent> where TBaseEvent : Event
  {
    /// <summary>
    /// Rehydrate <see cref="Aggregate"/>
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
    /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> or null when not found</returns>
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();

    /// <summary>
    /// Rehydrate <see cref="Aggregate"/> up to a certain date
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
    /// <param name="date">Date up to which to rehydrate <see cref="Aggregate"/></param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
    /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> as it was on <c>date</c> or null when not found</returns>
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
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
    
    /// <summary>
    /// Rehydrate and Persist <see cref="Aggregate"/> up to a certain date
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate"/> to rehydrate</param>
    /// <param name="date">Date up to which to rehydrate <see cref="Aggregate"/></param>
    /// <param name="action"><c>Action</c> to take before Persisting</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
    /// <returns><see cref="Aggregate"/> of type <see cref="TAggregate"/> or null when not found</returns>
    public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, DateTimeOffset date, Action<TAggregate> action,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var aggregate = await RehydrateAsync<TAggregate>(aggregateId, date, cancellationToken);
      action.Invoke(aggregate);
      return await PersistAsync(aggregate, cancellationToken);
    }
  }
}