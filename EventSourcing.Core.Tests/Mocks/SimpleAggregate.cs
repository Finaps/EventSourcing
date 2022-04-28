namespace Finaps.EventSourcing.Core.Tests.Mocks;

public record SimpleEvent : Event<SimpleAggregate>;

public class SimpleAggregate : Aggregate<SimpleAggregate>
{
  public int Counter { get; private set; }

  protected override void Apply(Event<SimpleAggregate> e) => Counter++;
}
