using System.IO;
using System.Text.Json;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract class RecordMigrationTests
{
  protected abstract IRecordStore RecordStore { get; }
  protected abstract IAggregateService AggregateService { get; }

  private class InvalidEventMigrator : EventMigrator<EmptyEvent, EmptyEvent>
  {
    protected override EmptyEvent Convert(EmptyEvent e) => throw new NotImplementedException();
  }
  
  private class InvalidPingEventMigrator : EventMigrator<EmptyEvent, MockEvent>
  {
    protected override MockEvent Convert(EmptyEvent e) => throw new NotImplementedException();
  }
  
  private class InvalidPongEventMigrator : EventMigrator<MockEvent, EmptyEvent>
  {
    protected override EmptyEvent Convert(MockEvent e) => throw new NotImplementedException();
  }

  [Fact]
  public Task Cannot_Create_Record_Migrator_With_Source_Equals_Target()
  {
    Assert.Throws<ArgumentException>(() => new InvalidEventMigrator());
    return Task.CompletedTask;
  }

  [Fact]
  public Task Cannot_Create_Record_Migrator_With_Cyclic_Reference()
  {
    Assert.Throws<ArgumentException>(() => new RecordConverter<Event>(new RecordConverterOptions
    {
      MigratorTypes = new List<Type> { typeof(InvalidPingEventMigrator), typeof(InvalidPongEventMigrator) }
    }));

    return Task.CompletedTask;
  }

  [Fact]
  public async Task Can_Migrate_Old_Event()
  {
    var someId = Guid.NewGuid();
    var aggregate = new MigratedAggregate();
    aggregate.Apply(new MigrationEventV2(someId));
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
    aggregate.Apply(new MigrationEvent(someId.ToString()));
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
    aggregate.Apply(e);
    var converter = new RecordConverter<Event>();
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
    Assert.Equal(e.SomeId, converted.SomeId);
    Assert.Equal(e.SomeString, converted.SomeString);
    Assert.Equal(e.SomeInt, converted.SomeInt);
    Assert.Null(converted.AddedField);
  }
}