using System.Reflection;

namespace EventSourcing.Core;

/// <summary>
/// An <see cref="Aggregate{TAggregate}"/> represents an aggregation of one or more <see cref="Event"/>s.
/// </summary>
/// <remarks>
/// <para>
/// To create a new Aggregate type, subclass <see cref="Aggregate{TAggregate}"/> and implement the <see cref="Apply"/> method
/// for every applicable <see cref="Event"/>.
/// </para>
/// <para>
/// Aggregates should only be updated by adding <see cref="Event"/>s to it using the <see cref="Apply{TEvent}"/> method.
/// </para>
/// <para>
/// To Persist and Rehydrate Aggregates, please refer to the <see cref="IAggregateService"/>.
/// </para>
/// </remarks>
/// <seealso cref="Event"/>
/// <seealso cref="IAggregateService"/>
public abstract class Aggregate : IHashable
{
  /// <summary>
  /// String representation of Aggregate Type
  /// </summary>
  /// <remarks>
  /// Equal to <c>GetType().Name</c>
  /// </remarks>
  /// <remarks>
  /// All <see cref="Event"/>s added to this Aggregate will have set <c>Event.AggregateType = Aggregate.Type</c>
  /// </remarks>
  public string Type { get; init; }
  
  /// <summary>
  /// Unique Partition identifier. Defaults to <see cref="Guid"/>.<see cref="Guid.Empty"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Call <c>new MyAggregate { PartitionId = myPid };</c> to construct an Aggregate with a specific <see cref="PartitionId"/>.
  /// All <see cref="Event"/>s that are added to this Aggregate will have set <c>Event.PartitionId = Aggregate.PartitionId</c>.
  /// </para>
  /// <para>
  /// <see cref="PartitionId"/> is mapped directly to CosmosDB's <c>PartitionKey</c>.
  /// See https://docs.microsoft.com/en-us/azure/cosmos-db/partitioning-overview for more information.
  /// </para>
  /// <para>
  /// <see cref="IAggregateTransaction"/> is scoped to <see cref="PartitionId"/>,
  /// i.e. no transactions involving multiple <see cref="PartitionId"/>'s can be committed.
  /// </para>
  /// </remarks>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Unique Aggregate identifier. Defaults to <see cref="Guid"/>.<see cref="Guid.NewGuid"/>.
  /// </summary>
  /// <remarks>
  /// All <see cref="Event"/>s added to this Aggregate will have set <c>Event.AggregateId = Aggregate.Id</c>
  /// </remarks>
  public Guid Id { get; init; }
  
  /// <summary>
  /// The number of <see cref="Event"/>s applied to this Aggregate.
  /// </summary>
  public long Version { get; protected set; }
  
  /// <summary>
  /// <see cref="Event"/>s that are not yet committed to the <see cref="IRecordStore"/>.
  /// </summary>
  /// <remarks>
  /// To commit these <see cref="Event"/>s, call <see cref="IAggregateService"/>.<see cref="IAggregateService.PersistAsync{TAggregate}"/>
  /// </remarks>
  [JsonIgnore] internal readonly List<Event> UncommittedEvents = new();

  /// <summary>
  /// Create new Aggregate with <c>Id = </c><see cref="Guid"/>.<see cref="Guid.NewGuid"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// The <see cref="IAggregateService"/> expects Aggregates to always have a default constructor.
  /// </para>
  /// <para>
  /// To create/update this Aggregate, rather than defining a custom constructor,
  /// Add <see cref="Event"/>s to it using the <see cref="Apply{TEvent}"/> method
  /// and resolve them using the <see cref="Apply"/> method.
  /// </para>
  /// </remarks>
  protected Aggregate()
  {
    Id = Guid.NewGuid();
    Type = GetType().Name;
  }

  internal abstract Task RehydrateAsync(Snapshot? snapshot, IAsyncEnumerable<Event> events, CancellationToken cancellationToken = default);
  public abstract string ComputeHash();
}

/// <summary>
/// An <see cref="Aggregate{TAggregate}"/> represents an aggregation of one or more <see cref="Event"/>s.
/// </summary>
/// <remarks>
/// <para>
/// To create a new Aggregate type, subclass <see cref="Aggregate{TAggregate}"/> and implement the <see cref="Apply"/> method
/// for every applicable <see cref="Event"/>.
/// </para>
/// <para>
/// Aggregates should only be updated by adding <see cref="Event"/>s to it using the <see cref="Apply{TEvent}"/> method.
/// </para>
/// <para>
/// To Persist and Rehydrate Aggregates, please refer to the <see cref="IAggregateService"/>.
/// </para>
/// </remarks>
/// <seealso cref="Event"/>
/// <seealso cref="IAggregateService"/>
public abstract class Aggregate<TAggregate> : Aggregate where TAggregate : Aggregate, new()
{
  internal override async Task RehydrateAsync(Snapshot? snapshot, IAsyncEnumerable<Event> events, CancellationToken cancellationToken = default)
  {
    if (snapshot != null) 
      ValidateAndApply((Snapshot<TAggregate>)snapshot);
    
    await foreach (var e in events.WithCancellation(cancellationToken))
      ValidateAndApply((Event<TAggregate>)e);
  }
  
  /// <summary>
  /// Apply <see cref="Event"/> to <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <remarks>
  /// To commit these <see cref="Event"/>s to the <see cref="IRecordStore"/>,
  /// call <see cref="IAggregateService"/>.<see cref="IAggregateService.PersistAsync{TAggregate}"/>
  /// </remarks>
  /// <param name="e"><see cref="Event"/> to apply</param>
  /// <typeparam name="TEvent"><see cref="Event"/> Type</typeparam>
  /// <returns>
  /// Copy of added <see cref="Event"/> with updated
  /// <see cref="Event.PartitionId"/>, <see cref="Event.AggregateId"/>, <see cref="Event.AggregateType"/> and <see cref="Event.Index"/>
  /// </returns>
  /// <exception cref="ArgumentException">Thrown when an invalid <see cref="Event"/> is added.</exception>
  /// <exception cref="ArgumentException">Thrown when a <see cref="Snapshot"/> is added.</exception>
  public TEvent Apply<TEvent>(TEvent e) where TEvent : Event<TAggregate>
  {
    e = e with
    {
      PartitionId = PartitionId,
      AggregateId = Id,
      AggregateType = Type,
      Index = Version
    };
    
    ValidateAndApply(e);
    UncommittedEvents.Add(e);
    return e;
  }
  
  /// <summary>
  /// Apply <see cref="Event"/> to <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <remarks>
  /// Use this method to add aggregation logic to your aggregate.
  /// </remarks>
  /// <example>
  /// <code>
  /// protected override void Apply(Event e)
  /// {
  ///  switch (e)
  ///  {
  ///   case EventType1 e1:
  ///     // Update according to e1
  ///     break;
  ///   case EventType2 e2:
  ///     // Update according to e2
  ///     break;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// <param name="e"><see cref="Event"/> to apply</param>
  protected abstract void Apply(Event<TAggregate> e);

  protected virtual void Apply(Snapshot<TAggregate> s) { }

  private void ValidateAndApply(Event e)
  {
    if (e is not Event<TAggregate> @event)
      throw new RecordValidationException($"{e} does not derive from {typeof(Event<TAggregate>)}");
    
    RecordValidation.ValidateEventForAggregate(this, e);
    
    Apply(@event);
    Version++;
  }

  private void ValidateAndApply(Snapshot s)
  {
    if (s is not Snapshot<TAggregate> @snapshot)
      throw new RecordValidationException($"{s} does not derive from {typeof(Event<TAggregate>)}");
    
    RecordValidation.ValidateSnapshotForAggregate(this, s);
    Apply(@snapshot);
    Version = s.Index + 1;
  }

  /// <summary>
  /// Compute Hash representing the logic of this <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Apply"/> method.
  /// The hash is used to determine if projections created from this <see cref="Aggregate{TAggregate}"/> are up to date.
  /// </summary>
  /// <remarks>
  /// If the <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Apply"/> method relies on domain logic not directly present in
  /// the <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Apply"/> body, consider overwriting <see cref="ComputeHash"/> to
  /// include the dependent methods in the hash calculation.
  /// </remarks>
  /// <seealso cref="Projection"/>
  /// <seealso cref="ProjectionFactory{TAggregate,TProjection}"/>
  /// <seealso cref="IHashable"/>
  /// <returns></returns>
  public override string ComputeHash() => IHashable.CombineHashes(
    IHashable.ComputeMethodHash(GetType().GetMethod(nameof(Apply),
      types: new[] { typeof(Event<TAggregate>) },
      bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic)),
    IHashable.ComputeMethodHash(GetType().GetMethod(nameof(Apply),
      types: new[] { typeof(Snapshot<TAggregate>) },
      bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic)));

}