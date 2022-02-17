using EventSourcing.Core.Records;

namespace EventSourcing.Core.Tests.Mocks;

public record VerboseAggregate : Aggregate
{
  public readonly List<Event> AppliedEvents = new();

  protected override void Apply(Event e) => AppliedEvents.Add(e);
}