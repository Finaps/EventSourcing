using System;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF.Tests.Mocks;

public record ReferenceEvent(Guid ReferenceAggregateId, Guid? EmptyAggregateId = null) : Event<ReferenceAggregate>;

public record ReferenceProjection(Guid ReferenceAggregateId, Guid? EmptyAggregateId = null) : Projection;

public class ReferenceAggregate : Aggregate<ReferenceAggregate>
{
  public Guid ReferenceAggregateId { get; private set; }
  public Guid? EmptyAggregateId { get; private set; }

  protected override void Apply(Event<ReferenceAggregate> e)
  {
    switch (e)
    {
      case ReferenceEvent reference:
        ReferenceAggregateId = reference.ReferenceAggregateId;
        EmptyAggregateId = reference.EmptyAggregateId;
        break;
    }
  }
}

public class ReferenceProjectionFactory : ProjectionFactory<ReferenceAggregate, ReferenceProjection>
{
  protected override ReferenceProjection CreateProjection(ReferenceAggregate aggregate) =>
    new(aggregate.ReferenceAggregateId, aggregate.EmptyAggregateId);
}
