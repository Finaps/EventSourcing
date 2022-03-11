namespace EventSourcing.Core;

/// <summary>
/// Record Validation
/// </summary>
public static class RecordValidation
{
  /// <summary>
  /// Validate Snapshot
  /// </summary>
  /// <param name="partitionId">Partition Id <see cref="Snapshot"/> is expected to have</param>
  /// <param name="snapshot"><see cref="Snapshot"/> to validate</param>
  public static void ValidateSnapshot(Guid partitionId, Snapshot snapshot)
  {
    if (snapshot.PartitionId != partitionId)
      Throw(snapshot, $"Snapshot PartitionId ('{snapshot.PartitionId}') not equal to Transaction PartitionId ('{partitionId}')");
      
    ValidateRecord(snapshot);
  }

  /// <summary>
  /// Validate <see cref="Event"/> sequence
  /// </summary>
  /// <param name="partitionId">Partition Id this sequence is expected to be in</param>
  /// <param name="events"><see cref="Event"/>s to validate</param>
  public static void ValidateEventSequence(Guid partitionId, IList<Event> events)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));

    foreach (var e in events)
      ValidateRecord(e);

    const string message = "Error Validating Event Sequence. ";

    var partitionIds = events.Select(x => x.PartitionId).Distinct().ToList();

    if (partitionIds.Count > 1)
      throw new RecordValidationException(message + $"All Events must share the same PartitionId. Expected {partitionId}, Found [ {string.Join(", ", events.Select(x => x.PartitionId))} ]");
    
    if (partitionIds.Single() != partitionId)
      throw new RecordValidationException(message + $"All Events in a transaction must share the same PartitionId. Expected {partitionId}, Found [ {string.Join(", ", events.Select(x => x.PartitionId))} ]");

    if (events.Select(x => x.AggregateId).Distinct().Count() > 1)
      throw new RecordValidationException(message + $"All Events must share the same AggregateId. Found [ {string.Join(", ", events.Select(x => x.AggregateId))} ]");

    if (!IsConsecutive(events.Select(e => e.Index).ToList()))
      throw new RecordValidationException(message + $"Event indices must be consecutive. Found [ {string.Join(", ", events.Select(x => x.Index))} ]");
  }

  /// <summary>
  /// Validate compatibility of <see cref="Snapshot"/> with <see cref="Aggregate"/>
  /// </summary>
  /// <param name="a"><see cref="Aggregate"/></param>
  /// <param name="s"><see cref="Snapshot"/></param>
  public static void ValidateSnapshotForAggregate(Aggregate a, Snapshot s)
  {
    ValidateRecordForAggregate(a, s);
  }

  /// <summary>
  /// Validate compatibility of <see cref="Event"/> with <see cref="Aggregate"/>
  /// </summary>
  /// <param name="a"><see cref="Aggregate"/></param>
  /// <param name="e"><see cref="Event"/></param>
  public static void ValidateEventForAggregate(Aggregate a, Event e)
  {
    ValidateRecordForAggregate(a, e);
    
    if (e.Index != a.Version)
      Throw(e, $"{e.Type}.Index ({e.Index}) does not correspond with {a.Type}.Version ({a.Version})");
  }

  private static void ValidateRecordForAggregate(Aggregate a, Event r)
  {
    ValidateRecord(r);

    if (r.AggregateId != a.Id)
      Throw(r, $"{r.Type}.AggregateId ({r.AggregateId}) should equal {a.Type}.Id ({a.Id})");

    if (r.AggregateType != a.Type)
      Throw(r, $"{r.Type}.AggregateType ({r.AggregateType}) should equal ({a.Type}).Type ({a.Type})");
    
    if (r.PartitionId != a.PartitionId)
      Throw(r, $"{r.Type}.PartitionId ({r.PartitionId}) should equal {a.Type}.PartitionId ({a.PartitionId})");
  }
  
  private static void ValidateRecord(Event r)
  {
    if (r.AggregateId == Guid.Empty)
      Throw(r, $"{r.Type}.AggregateId should not be Guid.Empty");

    if (string.IsNullOrEmpty(r.AggregateType))
      Throw(r, $"{r.Type}.AggregateType should not be null or empty");
    
    if (r.Index < 0)
      Throw(r, $"{r.Type}.Index ({r.Index}) must be a non-negative integer");
    
    var typeString = RecordTypeCache.GetAssemblyRecordTypeString(r.GetType());

    if(r.Type != typeString)
      Throw(r, $"{r.GetType().Name}.Type ({r.Type}) should equal to {typeString}");
  }

  private static void Throw(Event r, string message) =>
    throw new RecordValidationException($"Error Validating {r}: {message}");
  
  private static bool IsConsecutive(IList<long> numbers)
  {
    if (numbers.Count == 0) return true;

    var last = numbers[0];
    
    foreach (var number in numbers.Skip(1))
      if (number - last != 1) return false;
      else last = number;

    return true;
  }
}
