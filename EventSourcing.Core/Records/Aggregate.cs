namespace EventSourcing.Core.Records;

/// <summary>
/// Abstract Base <see cref="Aggregate"/>
/// </summary>
public abstract record Aggregate : Record
{
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
  /// Apply Event
  /// </summary>
  /// <param name="e"><see cref="Event"/> to apply</param>
  protected abstract void Apply(Event e);

  /// <summary>
  /// Snapshot interval length
  /// </summary>
  public virtual long SnapshotInterval { get; }
  
  /// <summary>
  /// If true, persisting this Aggregate will store an Aggregate View
  /// </summary>
  public virtual bool ShouldStoreAggregateView { get; }
  
  /// <summary>
  /// Create Snapshot
  /// </summary>
  /// <returns><see cref="Snapshot"/></returns>
  protected virtual Snapshot CreateSnapshot() =>
    throw new NotImplementedException(
      "Error creating snapshot. " +
      $"{GetType()}.{nameof(CreateSnapshot)} is not implemented. " +
      $"{nameof(CreateSnapshot)} and {nameof(ApplySnapshot)} should be implemented when {nameof(SnapshotInterval)} > 0.");
  
  /// <summary>
  /// Apply Snapshot
  /// </summary>
  /// <param name="s"><see cref="Snapshot"/> to apply</param>
  protected virtual void ApplySnapshot(Snapshot s) =>
    throw new NotImplementedException(
      "Error applying snapshot. " +
      $"{GetType()}.{nameof(ApplySnapshot)} is not implemented. " +
      $"{nameof(CreateSnapshot)} and {nameof(ApplySnapshot)} should be implemented when {nameof(SnapshotInterval)} > 0.");

  /// <summary>
  /// Create Snapshot
  /// </summary>
  /// <returns></returns>
  public Snapshot CreateLinkedSnapshot()
  {
    if (Version == 0)
      throw new InvalidOperationException(
        "Error creating snapshot. Snapshots are undefined for aggregates with version 0.");
    
    return CreateSnapshot() with
    {
      PartitionId = PartitionId,
      AggregateId = Id,
      AggregateType = Type,
      Index = Version - 1
    };
  } 
    
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
  /// Clear Uncommitted Events
  /// </summary>
  public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
  
  /// <summary>
  /// Calculates if the snapshot interval has been exceeded (and a snapshot thus has to be created)
  /// </summary>
  /// <returns></returns>
  public bool IsSnapshotIntervalExceeded() => SnapshotInterval != 0 &&
                                          (UncommittedEvents.First().Index + 1) / SnapshotInterval !=
                                          (UncommittedEvents.Last().Index + 1) / SnapshotInterval;

  private void ValidateAndApplyEvent(Event e)
  {
    RecordValidation.ValidateEventForAggregate(this, e);
    Apply(e);
    Version++;
  }

  private void ValidateAndApplySnapshot(Snapshot s)
  {
    RecordValidation.ValidateSnapshotForAggregate(this, s);
    ApplySnapshot(s);
    Version = s.Index + 1;
  }
}