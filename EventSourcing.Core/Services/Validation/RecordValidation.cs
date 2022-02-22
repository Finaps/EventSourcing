namespace EventSourcing.Core;

public static class RecordValidation
{
  public static void ValidateSnapshot(Guid partitionId, Snapshot snapshot)
  {
    if (snapshot.PartitionId != partitionId)
      Throw(snapshot, $"Snapshot PartitionId ('{snapshot.PartitionId}') not equal to Transaction PartitionId ('{partitionId}')");
      
    ValidateRecord(snapshot);
  }
  
  public static void ValidateSnapshot(Snapshot snapshot)
  {
    ValidateRecord(snapshot);
  }
  
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

    if (events.Select(x => x.RecordId).Distinct().Count() != events.Count)
      throw new RecordValidationException(message + $"All Events should have unique RecordIds. Found [ {string.Join(", ", events.Select(x => x.RecordId))} ]");

    if (!IsConsecutive(events.Select(e => e.Index).ToList()))
      throw new RecordValidationException(message + $"Event indices must be consecutive. Found [ {string.Join(", ", events.Select(x => x.Index))} ]");
  }

  public static void ValidateRecord(Event r)
  {
    if (r.AggregateId == Guid.Empty)
      Throw(r, $"{r.Type}.AggregateId should not be Guid.Empty");
    
    if (r.RecordId == Guid.Empty)
      Throw(r, $"{r.Type}.RecordId should not be Guid.Empty");
    
    if (string.IsNullOrEmpty(r.AggregateType))
      Throw(r, $"{r.Type}.AggregateType should not be null or empty");
    
    if (r.Index < 0)
      Throw(r, $"{r.Type}.Index ({r.Index}) must be a non-negative integer");
    
    var typeString = RecordTypeCache.GetAssemblyRecordTypeString(r.GetType());

    if(r.Type != typeString)
      Throw(r, $"{r.GetType().Name}.Type ({r.Type}) should equal to {typeString}");
  }

  public static void ValidateSnapshotForAggregate(Aggregate a, Snapshot s)
  {
    ValidateRecordForAggregate(a, s);
  }

  public static void ValidateEventForAggregate(Aggregate a, Event e)
  {
    ValidateRecordForAggregate(a, e);
    
    if (e.Index != a.Version)
      Throw(e, $"{e.Type}.Index ({e.Index}) does not correspond with {a.Type}.Version ({a.Version})");
  }

  public static void ValidateRecordForAggregate(Aggregate a, Event r)
  {
    ValidateRecord(r);

    if (r.AggregateId != a.Id)
      Throw(r, $"{r.Type}.AggregateId ({r.AggregateId}) should equal {a.Type}.Id ({a.Id})");

    if (r.AggregateType != a.Type)
      Throw(r, $"{r.Type}.AggregateType ({r.AggregateType}) should equal ({a.Type}).Type ({a.Type})");
    
    if (r.PartitionId != a.PartitionId)
      Throw(r, $"{r.Type}.PartitionId ({r.PartitionId}) should equal {a.Type}.PartitionId ({a.PartitionId})");
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
