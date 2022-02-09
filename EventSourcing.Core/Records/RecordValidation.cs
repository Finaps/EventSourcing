using System.Reflection;
using EventSourcing.Core.Types;

namespace EventSourcing.Core;

public static class RecordValidation
{
  public static void ValidateSnapshot(Snapshot snapshot)
  {
    ValidateRecord(snapshot);
  }
  
  public static void ValidateEventSequence(Guid partitionId, IList<Event> events)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));

    foreach (var e in events)
      ValidateRecord(e);

    const string message = "Error Validating Event Sequence: ";

    var partitionIds = events.Select(x => x.PartitionId).Distinct().ToList();

    if (partitionIds.Count > 1)
      throw new RecordValidationException(message + "All Events must share the same PartitionId");
    
    if (partitionIds.Single() != partitionId)
      throw new RecordValidationException(message + "All Events in a transaction must share the same PartitionId");

    if (events.Select(x => x.AggregateId).Distinct().Count() > 1)
      throw new RecordValidationException(message + "All Events must share the same AggregateId");

    if (events.Select(x => x.RecordId).Distinct().Count() != events.Count)
      throw new RecordValidationException(message + "All Events should have unique RecordIds");

    if (!IsConsecutive(events.Select(e => e.Index).ToList()))
      throw new RecordValidationException(message + "Event indices must be consecutive");
  }

  public static void ValidateRecord(Record r)
  {
    if (r.AggregateId == Guid.Empty)
      Throw(r, "AggregateId should not be empty");
    
    if (r.RecordId == Guid.Empty)
      Throw(r, "RecordId should not be empty");
    
    if (string.IsNullOrEmpty(r.AggregateType))
      Throw(r, "AggregateType should not be null or empty");
    
    if (r.Index < 0)
      Throw(r, "Index must be a non-negative integer");
    
    var recordType = RecordTypeProvider.Instance.Initialized ?
      RecordTypeProvider.Instance.GetRecordTypeString(r.GetType()) :
      r.GetType().GetCustomAttribute<RecordType>()?.Value ?? r.GetType().Name;
    
    if (r.Type != recordType)
      Throw(r, $"Type ({r.Type}) does not correspond with record Type ({recordType})");
  }

  public static void ValidateSnapshotForAggregate(Aggregate a, Snapshot s)
  {
    ValidateRecordForAggregate(a, s);
  }

  public static void ValidateEventForAggregate(Aggregate a, Event e)
  {
    var aggregateType = a.GetType().Name;
    var eventType = e.GetType().Name;
    
    ValidateRecordForAggregate(a, e);
    
    if (e.Index != a.Version)
      Throw(e, $"{eventType}.Index ({e.Index}) does not correspond with {aggregateType}.Version ({a.Version})");
  }

  public static void ValidateRecordForAggregate(Aggregate a, Record r)
  {
    var aggregateType = a.GetType().Name;

    ValidateRecord(r);

    if (r.AggregateId != a.Id)
      Throw(r, $"AggregateId ({r.AggregateId}) does not correspond with {aggregateType}.Id ({a.Id})");

    if (r.AggregateType != aggregateType)
      Throw(r, $"AggregateType ({r.AggregateType}) does not correspond with typeof(Aggregate) ({aggregateType})");
    
    if (r.PartitionId != a.PartitionId)
      Throw(r, $"PartitionId ({r.PartitionId}) does not correspond with {aggregateType}.PartitionId ({a.PartitionId})");
  }

  private static void Throw(Record r, string message) =>
    throw new RecordValidationException($"Error Validating {r.GetType().Name} with RecordId '{r.RecordId}': {message}");
  
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
