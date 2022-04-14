using System;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Tests.Mocks;
using EventSourcing.EF.Tests.Mocks;
using Xunit;

namespace EventSourcing.EF.Tests;

public abstract partial class EntityFrameworkEventSourcingTests
{
  [Fact]
  public async Task AggregateReferenceTests_Can_Reference_Self()
  {
    var referenced = new ReferenceAggregate();
    referenced.Apply(new ReferenceEvent
    {
      ReferenceAggregateId = referenced.Id
    });
    await AggregateService.PersistAsync(referenced);
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Can_Reference_Other_Aggregate()
  {
    var referenced = new ReferenceAggregate();
    referenced.Apply(new ReferenceEvent
    {
      ReferenceAggregateId = referenced.Id
    });
    await AggregateService.PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent
    {
      ReferenceAggregateId = referenced.Id
    });

    await AggregateService.PersistAsync(aggregate);
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Can_Reference_Other_Aggregate_Type()
  {
    var referenced = new EmptyAggregate();
    referenced.Apply(new EmptyEvent());
    await AggregateService.PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent
    {
      ReferenceAggregateId = aggregate.Id,
      EmptyAggregateId = referenced.Id
    });

    await AggregateService.PersistAsync(aggregate);
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Cannot_Reference_Wrong_Aggregate_Type()
  {
    var referenced = new SimpleAggregate();
    referenced.Apply(new SimpleEvent());
    await AggregateService.PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent
    {
      ReferenceAggregateId = aggregate.Id,
      EmptyAggregateId = referenced.Id
    });

    await Assert.ThrowsAsync<RecordStoreException>(async () => await AggregateService.PersistAsync(aggregate));
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Cannot_Reference_NonExisting_Aggregate()
  {
    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent
    {
      ReferenceAggregateId = Guid.NewGuid()
    });

    await Assert.ThrowsAsync<RecordStoreException>(async () => await AggregateService.PersistAsync(aggregate));
  }
}