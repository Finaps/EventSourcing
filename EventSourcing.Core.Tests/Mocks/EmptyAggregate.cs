namespace EventSourcing.Core.Tests.Mocks;

public record EmptyAggregate : Aggregate
{
  protected override void Apply(Event e) {}
}