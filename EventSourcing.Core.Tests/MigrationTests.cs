using System.IO;
using System.Text.Json;
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
    
    [Fact]
    public void Can_Convert_Trivial_Migration()
    {
        var e = new TrivialMigrationEventOriginal(Guid.NewGuid(), "something", 42, 42.00m);
        var aggregate = new MigratedAggregate();
        aggregate.Add(e);
        var converter = new EventConverter<Event>();
        var options = new JsonSerializerOptions { WriteIndented = true };
        
        using var writeStream = new MemoryStream();
        converter.Write(new Utf8JsonWriter(writeStream), aggregate.UncommittedEvents.Single(), options);
        writeStream.Position = 0;
        var json = System.Text.Encoding.UTF8.GetString(writeStream.ToArray());
        // Manipulate JSON string so that its type field has the correct type to convert to
        var adjustedJson = json.Replace(nameof(TrivialMigrationEventOriginal), nameof(TrivialMigrationEvent));

        var bytes = System.Text.Encoding.UTF8.GetBytes(adjustedJson);
        var reader = new Utf8JsonReader(bytes, true, default);
        var converted = converter.Read(ref reader, typeof(TrivialMigrationEvent), options) as TrivialMigrationEvent;
            
        Assert.NotNull(converted);
        Assert.Equal(e.someId,converted.someId);
        Assert.Equal(e.someString,converted.someString);
        Assert.Equal(e.someInt,converted.someInt);
        Assert.Null(converted.addedField);
    }
}