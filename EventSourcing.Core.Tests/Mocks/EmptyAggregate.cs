namespace Finaps.EventSourcing.Core.Tests.Mocks;

public record EmptyEvent : Event<EmptyAggregate>;

public record EmptySnapshot : Snapshot<EmptyAggregate>;

public record EmptyProjection : Projection;

public record AnotherEmptyProjection : Projection;

public record NullProjection : Projection;

public class EmptyAggregate : Aggregate<EmptyAggregate>
{
  protected override void Apply(Event<EmptyAggregate> e) {}
}

public class EmptyProjectionFactory : ProjectionFactory<EmptyAggregate, EmptyProjection>
{
  protected override EmptyProjection CreateProjection(EmptyAggregate aggregate) => new ();
}

public class AnotherEmptyProjectionFactory : ProjectionFactory<EmptyAggregate, AnotherEmptyProjection>
{
  protected override AnotherEmptyProjection CreateProjection(EmptyAggregate aggregate) => new ();
}

public class NullProjectionFactory : ProjectionFactory<EmptyAggregate, NullProjection>
{
  protected override NullProjection? CreateProjection(EmptyAggregate aggregate) => null;
}
