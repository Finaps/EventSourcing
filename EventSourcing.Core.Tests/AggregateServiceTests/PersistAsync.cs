namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task AggregateService_PersistAsync_Can_Persist_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new SimpleEvent());

    await AggregateService.PersistAsync(aggregate);

    var result = await RecordStore
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    Assert.NotNull(result);
  }
  
  [Fact]
  public async Task AggregateService_PersistAsync_Can_Persist_Multiple_Events()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent())
    };

    await AggregateService.PersistAsync(aggregate);

    var eventCount = await RecordStore
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count, eventCount);
  }
  
  [Fact]
  public async Task AggregateService_PersistAsync_Can_Snapshot_Aggregate()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await AggregateService.PersistAsync(aggregate);

    var snapshotResult = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(snapshotResult);
    Assert.Equal((int) factory.SnapshotInterval, eventCount);
  }
    
  [Fact]
  public async Task AggregateService_PersistAsync_Cannot_Snapshot_When_Interval_Is_Not_Exceeded()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval - 1;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await AggregateService.PersistAsync(aggregate);

    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .LongCountAsync();

    Assert.Equal(0, snapshotCount);
    Assert.Equal(eventsCount, eventCount);
  }
    
  [Fact]
  public async Task AggregateService_PersistAsync_One_Snapshot_When_Interval_Is_Exceeded_Twice()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = 2 * factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await AggregateService.PersistAsync(aggregate);
    
    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .LongCountAsync();
      
    Assert.Equal(1, snapshotCount);
    Assert.Equal(eventsCount, eventCount);
  }
  
  [Fact]
  public async Task AggregateService_PersistAsync_Can_Snapshot_Aggregate_When_Appending_One_Event()
  {
    // To test for an previous issue where snapshotting was not happening when exactly one event was persisted
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
    {
      aggregate.Apply(new SnapshotEvent());
      await AggregateService.PersistAsync(aggregate);
    }

    var snapshotResult = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(snapshotResult);
    Assert.Equal((int) factory.SnapshotInterval, eventCount);
  }

  [Fact] // Tests issue https://github.com/Finaps/EventSourcing/issues/72
  public async Task AggregateService_PersistAsync_NonAlphabetical_Events_Persisted_In_Order()
  {
    var bankAccount = new BankAccount();
    var bankAccount2 = new BankAccount();

    bankAccount.Apply(new BankAccountCreatedEvent("E. Sourcing", "Some IBAN"));
    bankAccount2.Apply(new BankAccountCreatedEvent("Other Person", "Some other IBAN"));
    
    bankAccount.Apply(new BankAccountFundsDepositedEvent(500));
    bankAccount.Apply(new BankAccountFundsWithdrawnEvent(100));
    bankAccount.Apply(new BankAccountFundsTransferredEvent(50, bankAccount.Id, bankAccount2.Id));
    bankAccount.Apply(new BankAccountFundsWithdrawnEvent(20));
    bankAccount.Apply(new BankAccountFundsDepositedEvent(500));
    await AggregateService.PersistAsync(bankAccount);
  }

  [Fact]
  public async Task AggregateService_PersistAsync_Can_Persist_Multiple_Projections()
  {
    var empty = new EmptyAggregate();
    empty.Apply(new EmptyEvent());
    await AggregateService.PersistAsync(empty);
    
    var emptyProjection = await RecordStore.GetProjectionByIdAsync<EmptyProjection>(empty.Id);
    var anotherEmptyProjection = await RecordStore.GetProjectionByIdAsync<AnotherEmptyProjection>(empty.Id);
    var nullProjection = await RecordStore.GetProjectionByIdAsync<NullProjection>(empty.Id);
    
    Assert.NotNull(emptyProjection);
    Assert.NotNull(anotherEmptyProjection);
    Assert.Null(nullProjection);
  }

  [Fact]
  public async Task AggregateService_PersistAsync_Can_Persist_Projection_Hierarchy()
  {
    var tph = new HierarchyAggregate();
    tph.Apply(new HierarchyEvent("Long String", "Tiny", "Small"));
    await AggregateService.PersistAsync(tph);

    var projectionA = await RecordStore.GetProjectionByIdAsync<HierarchyProjection>(tph.Id);
    Assert.IsType<HierarchyProjectionA>(projectionA);
    
    var projectionALiteral = await RecordStore.GetProjectionByIdAsync<HierarchyProjectionA>(tph.Id);
    Assert.IsType<HierarchyProjectionA>(projectionALiteral);

    tph.Apply(new HierarchyEvent("Tiny", "Long String", "Small"));
    await AggregateService.PersistAsync(tph);
    
    var projectionB = await RecordStore.GetProjectionByIdAsync<HierarchyProjection>(tph.Id);
    Assert.IsType<HierarchyProjectionB>(projectionB);

    tph.Apply(new HierarchyEvent("Small", "Tiny", "Long String"));
    await AggregateService.PersistAsync(tph);
    
    var projectionC = await RecordStore.GetProjectionByIdAsync<HierarchyProjection>(tph.Id);
    Assert.IsType<HierarchyProjectionC>(projectionC);
  }
}