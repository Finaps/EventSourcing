namespace EventSourcing.Core.Tests.Mocks;

public class VerboseAggregate : Aggregate
{
  public readonly List<Event> AppliedEvents = new();

  protected override void Apply(Event e) => AppliedEvents.Add(e);
}