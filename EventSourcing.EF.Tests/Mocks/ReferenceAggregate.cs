using System;
using EventSourcing.Core;

namespace EventSourcing.EF.Tests.Mocks;

public record ReferenceEvent : Event<ReferenceAggregate>
{
  public Guid ReferenceAggregateId { get; init; }
  public Guid? EmptyAggregateId { get; init; }
}

public class ReferenceAggregate : Aggregate<ReferenceAggregate>
{
  protected override void Apply(Event<ReferenceAggregate> e) { }
}
