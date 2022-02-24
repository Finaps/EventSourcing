namespace EventSourcing.Core.Tests.Mocks;

public record EmptyEvent : Event;

public record EmptyProjection : Projection;

public record EmptySnapshot : Snapshot;

public class EmptyAggregate : Aggregate
{
  protected override void Apply(Event e) {}
}

public class EmptyProjectionFactory : ProjectionFactory<EmptyAggregate, EmptyProjection>
{
  protected override EmptyProjection CreateProjection(EmptyAggregate aggregate) => new ();
}
