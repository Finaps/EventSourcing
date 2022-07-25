using System;
using System.Threading.Tasks;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Core.Tests.Mocks;
using Finaps.EventSourcing.EF.Tests.Mocks;
using Xunit;

namespace Finaps.EventSourcing.EF.Tests;

public abstract partial class EntityFrameworkEventSourcingTests
{
  [Fact]
  public async Task AggregateReferenceTests_Can_Reference_Self()
  {
    var referenced = new ReferenceAggregate();
    referenced.Apply(new ReferenceEvent(referenced.Id));
    await GetAggregateService().PersistAsync(referenced);
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Can_Reference_Other_Aggregate()
  {
    var referenced = new ReferenceAggregate();
    referenced.Apply(new ReferenceEvent(referenced.Id));
    await GetAggregateService().PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent(referenced.Id));
    await GetAggregateService().PersistAsync(aggregate);
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Can_Update_Reference()
  {
    var referenced = new ReferenceAggregate();
    referenced.Apply(new ReferenceEvent(referenced.Id));
    await GetAggregateService().PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent(referenced.Id));
    await GetAggregateService().PersistAsync(aggregate);
    
    var newReferenced = new ReferenceAggregate();
    newReferenced.Apply(new ReferenceEvent(newReferenced.Id));
    await GetAggregateService().PersistAsync(newReferenced);

    await GetAggregateService().RehydrateAndPersistAsync<ReferenceAggregate>(aggregate.Id, x =>
      x.Apply(new ReferenceEvent(newReferenced.Id)));
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Can_Reference_Other_Aggregate_Type()
  {
    var referenced = new EmptyAggregate();
    referenced.Apply(new EmptyEvent());
    await GetAggregateService().PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent(aggregate.Id, referenced.Id));
    await GetAggregateService().PersistAsync(aggregate);
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Cannot_Reference_Wrong_Aggregate_Type()
  {
    var referenced = new SimpleAggregate();
    referenced.Apply(new SimpleEvent());
    await GetAggregateService().PersistAsync(referenced);

    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent(aggregate.Id, referenced.Id));
    await Assert.ThrowsAsync<RecordStoreException>(async () => await GetAggregateService().PersistAsync(aggregate));
  }
  
  [Fact]
  public async Task AggregateReferenceTests_Cannot_Reference_NonExisting_Aggregate()
  {
    var aggregate = new ReferenceAggregate();
    aggregate.Apply(new ReferenceEvent(Guid.NewGuid(), null));
    await Assert.ThrowsAsync<RecordStoreException>(async () => await GetAggregateService().PersistAsync(aggregate));
  }
}