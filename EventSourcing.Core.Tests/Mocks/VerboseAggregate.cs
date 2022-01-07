namespace EventSourcing.Core.Tests.Mocks;

public class VerboseAggregate : Aggregate
{
  public List<Event> AppliedEvents = new();
  public bool IsFinished = false;
    
  protected override void Apply<TEvent>(TEvent e) => AppliedEvents.Add(e);
  protected override void Finish() => IsFinished = true;
}