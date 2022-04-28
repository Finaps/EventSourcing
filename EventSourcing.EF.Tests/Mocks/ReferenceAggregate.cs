using System;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF.Tests.Mocks;

public record ReferenceEvent : Event<ReferenceAggregate>
{
  public Guid ReferenceAggregateId { get; init; }
  public Guid? EmptyAggregateId { get; init; }
}

public class ReferenceAggregate : Aggregate<ReferenceAggregate>
{
  protected override void Apply(Event<ReferenceAggregate> e) { }
}
