using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract class MigrationTests
{
    protected abstract IEventStore EventStore { get; }
    protected abstract ISnapshotStore SnapshotStore { get; }
    protected abstract IAggregateService AggregateService { get; }

    [Fact]
    public async Task Can_Migrate_Old_Event()
    {
        var someId = Guid.NewGuid();
        var aggregate = new MigratedAggregate();
        aggregate.Add(new MigrationEventV2(someId));
        await AggregateService.PersistAsync(aggregate);
        var rehydrated = await AggregateService.RehydrateAsync<MigratedAggregate>(aggregate.Id);
        
        Assert.NotNull(rehydrated);
        Assert.Single(rehydrated.SomeIds);
        Assert.Equal(someId, rehydrated.SomeIds.Single());
    }
    
    [Fact]
    public async Task Can_Migrate_Old_Event_Twice()
    {
        var someId = Guid.NewGuid();
        var aggregate = new MigratedAggregate();
        aggregate.Add(new MigrationEvent(someId.ToString()));
        await AggregateService.PersistAsync(aggregate);
        var rehydrated = await AggregateService.RehydrateAsync<MigratedAggregate>(aggregate.Id);
        
        Assert.NotNull(rehydrated);
        Assert.Single(rehydrated.SomeIds);
        Assert.Equal(someId, rehydrated.SomeIds.Single());
    }
}