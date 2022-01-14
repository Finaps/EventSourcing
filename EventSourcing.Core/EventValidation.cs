namespace EventSourcing.Core;

public static class EventValidation
{
  public static void Validate<TBaseEvent>(Guid partitionId, IList<TBaseEvent> events) where TBaseEvent : Event, new()
  {
    if (events == null) throw new ArgumentNullException(nameof(events));

    if (events.Select(x => x.PartitionId).Distinct().SingleOrDefault() != partitionId)
      throw new ArgumentException("All Events in a Transaction should share the same PartitionId", nameof(events));

    var aggregateIds = events.Select(x => x.AggregateId).Distinct().ToList();

    if (aggregateIds.Count > 1)
      throw new ArgumentException("All Events should have the same AggregateId", nameof(events));

    if (aggregateIds.Single() == Guid.Empty)
      throw new ArgumentException("AggregateId should be set, did you forget to Add Events to an Aggregate?", nameof(events));

    if (!IsConsecutive(events.Select(e => e.AggregateVersion).ToList()))
      throw new ArgumentException("Event versions should be consecutive");
  }
  
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
