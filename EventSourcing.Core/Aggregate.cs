using System.Reflection;

namespace EventSourcing.Core;

/// <summary>
/// Abstract Base <see cref="Aggregate"/>
/// </summary>
public abstract class Aggregate : IHashable
{
  /// <summary>
  /// String representation of Record Type
  /// </summary>
  /// <remarks>
  /// Can be overridden using <see cref="RecordTypeAttribute"/>
  /// </remarks>
  public string Type { get; init; }
  
  /// <summary>
  /// Unique Partition identifier.
  /// </summary>
  /// <remarks>
  /// <see cref="IRecordTransaction"/> and <see cref="IAggregateTransaction"/> are scoped to <see cref="PartitionId"/>
  /// </remarks>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Unique Record identifier.
  /// </summary>
  public Guid Id { get; init; }
  
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; private set; }

  /// <summary>
  /// Uncommitted Events
  /// </summary>
  [JsonIgnore] public ImmutableArray<Event> UncommittedEvents => _uncommittedEvents.ToImmutableArray();
  [JsonIgnore] private readonly List<Event> _uncommittedEvents = new();
  
  /// <summary>
  /// Clear Uncommitted Events
  /// </summary>
  public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
  
  /// <summary>
  /// Create new Record
  /// </summary>
  protected Aggregate()
  {
    Id = Guid.NewGuid();
    Type = GetType().Name;
  }

  /// <summary>
  /// Apply Event
  /// </summary>
  /// <param name="e"><see cref="Event"/> to apply</param>
  protected abstract void Apply(Event e);

  /// <summary>
  /// Add Event to Aggregate
  /// </summary>
  /// <remarks>
  /// Will Apply <see cref="Event"/> and add it to UncommittedEvents.
  /// To Persist the aggregate, call <c>IEventService.PersistAsync()</c>.
  /// </remarks>
  /// <param name="e"><see cref="Event"/> to add</param>
  /// <typeparam name="TEvent"><see cref="Event"/> Type</typeparam>
  /// <returns>Added <see cref="Event"/></returns>
  /// <exception cref="ArgumentException">Thrown when an invalid <see cref="Event"/> is added.</exception>
  public TEvent Add<TEvent>(TEvent e) where TEvent : Event
  {
    if (e is Snapshot)
      throw new ArgumentException("Cannot 'Add' Snapshot to Aggregate");
    
    e = e with
    {
      PartitionId = PartitionId,
      AggregateId = Id,
      AggregateType = Type,
      Index = Version
    };
    
    ValidateAndApplyEvent(e);
    _uncommittedEvents.Add(e);
    return e;
  }

  /// <summary>
  /// Rehydrate <see cref="Aggregate"/> from <see cref="Snapshot"/> and <see cref="Event"/> stream.
  /// </summary>
  /// <param name="partitionId">Unique Aggregate Partition identifier</param>
  /// <param name="aggregateId">Unique Aggregate identifier</param>
  /// <param name="snapshot"><see cref="Snapshot"/></param>
  /// <param name="events"><see cref="Event"/> stream</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> Type</typeparam>
  /// <returns><see cref="Aggregate"/> of type <c>TAggregate</c></returns>
  /// <exception cref="ArgumentException">Thrown when <c>id</c> or <c>events</c> are invalid</exception>
  public static async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, Snapshot? snapshot,
    IAsyncEnumerable<Event> events, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    if (aggregateId == Guid.Empty)
      throw new ArgumentException($"Error Rehydrating {typeof(TAggregate)}. Aggregate Id should not be Guid.Empty. ", nameof(aggregateId));

    var aggregate = new TAggregate { PartitionId = partitionId, Id = aggregateId };
    
    if (snapshot != null)
      aggregate.ValidateAndApplySnapshot(snapshot);

    await foreach (var e in events.WithCancellation(cancellationToken))
      aggregate.ValidateAndApplyEvent(e);

    // If no Events have been applied (i.e. no events could be found), return null
    // Otherwise, return Aggregate
    return aggregate.Version == 0 ? null : aggregate;
  }
  
  /// <summary>
  /// Compute Hash representing the logic of this <see cref="Aggregate"/>.<see cref="Aggregate.Apply"/> method.
  /// The hash is used to determine if projections created from this <see cref="Aggregate"/> are up to date.
  /// </summary>
  /// <remarks>
  /// If the <see cref="Aggregate"/>.<see cref="Aggregate.Apply"/> method relies on domain logic not directly present in
  /// the <see cref="Aggregate"/>.<see cref="Aggregate.Apply"/> body, consider overwriting <see cref="ComputeHash"/> to
  /// include the dependent methods in the hash calculation.
  /// </remarks>
  /// <seealso cref="Projection"/>
  /// <seealso cref="ProjectionFactory{TAggregate,TProjection}"/>
  /// <seealso cref="IHashable"/>
  /// <returns></returns>
  public virtual string ComputeHash() => IHashable.ComputeMethodHash(
    GetType().GetMethod(nameof(Apply), BindingFlags.Instance | BindingFlags.NonPublic));

  private void ValidateAndApplyEvent(Event e)
  {
    RecordValidation.ValidateEventForAggregate(this, e);
    Apply(e);
    Version++;
  }

  private void ValidateAndApplySnapshot(Snapshot s)
  {
    RecordValidation.ValidateSnapshotForAggregate(this, s);
    Apply(s);
    Version = s.Index + 1;
  }
}