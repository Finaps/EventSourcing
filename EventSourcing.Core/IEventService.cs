using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IEventService : IEventService<Event> { }

  /// <summary>
  /// Event Service Interface: Rehydrating and Persisting <see cref="Aggregate{TBaseEvent}"/>s from <see cref="Event"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent">Base <see cref="Event"/> for <see cref="IEventService{TBaseEvent}"/></typeparam>
  public interface IEventService<TBaseEvent> where TBaseEvent : Event
  {
    /// <summary>
    /// Rehydrate <see cref="Aggregate{TBaseEvent}"/>
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TBaseEvent}"/> to rehydrate</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TBaseEvent}"/></typeparam>
    /// <returns><see cref="Aggregate{TBaseEvent}"/> of type <see cref="TAggregate"/></returns>
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();

    /// <summary>
    /// Rehydrate <see cref="Aggregate{TBaseEvent}"/> up to a certain date
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TBaseEvent}"/> to rehydrate</param>
    /// <param name="date">Date up to which to rehydrate <see cref="Aggregate{TBaseEvent}"/></param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TBaseEvent}"/></typeparam>
    /// <returns><see cref="Aggregate{TBaseEvent}"/> of type <see cref="TAggregate"/> as it was on <c>date</c></returns>
    Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();
    
    /// <summary>
    /// Persist <see cref="Aggregate{TBaseEvent}"/>
    /// </summary>
    /// <param name="aggregate"><see cref="Aggregate{TBaseEvent}"/> to persist</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TBaseEvent}"/></typeparam>
    /// <returns>Persisted <see cref="Aggregate{TBaseEvent}"/></returns>
    Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();

    /// <summary>
    /// Rehydrate and Persist <see cref="Aggregate{TBaseEvent}"/>
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TBaseEvent}"/> to rehydrate</param>
    /// <param name="action"><c>Action</c> to take before Persisting</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TBaseEvent}"/></typeparam>
    /// <returns><see cref="Aggregate{TBaseEvent}"/> of type <see cref="TAggregate"/></returns>
    public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, Action<TAggregate> action,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var aggregate = await RehydrateAsync<TAggregate>(aggregateId, cancellationToken);
      action.Invoke(aggregate);
      return await PersistAsync(aggregate, cancellationToken);
    }
    
    /// <summary>
    /// Rehydrate and Persist <see cref="Aggregate{TBaseEvent}"/> up to a certain date
    /// </summary>
    /// <param name="aggregateId">Unique identifier of <see cref="Aggregate{TBaseEvent}"/> to rehydrate</param>
    /// <param name="date">Date up to which to rehydrate <see cref="Aggregate{TBaseEvent}"/></param>
    /// <param name="action"><c>Action</c> to take before Persisting</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate">Type of <see cref="Aggregate{TBaseEvent}"/></typeparam>
    /// <returns><see cref="Aggregate{TBaseEvent}"/> of type <see cref="TAggregate"/></returns>
    public async Task<TAggregate> RehydrateAndPersistAsync<TAggregate>(Guid aggregateId, DateTimeOffset date, Action<TAggregate> action,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var aggregate = await RehydrateAsync<TAggregate>(aggregateId, date, cancellationToken);
      action.Invoke(aggregate);
      return await PersistAsync(aggregate, cancellationToken);
    }
  }
}