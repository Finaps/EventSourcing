namespace EventSourcing.Core.Tests.Mocks;

public class VerboseAggregate : Aggregate
{
  public readonly List<Event> AppliedEvents = new();
  public bool IsFinished;
    
  protected override void Apply(Event e) => AppliedEvents.Add(e);
  protected override void Finish() => IsFinished = true;
}