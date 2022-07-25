namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_AddEventsAsync_Can_Add_Single_Event()
  {
    await GetRecordStore().AddEventsAsync(new [] { new EmptyAggregate().Apply(new EmptyEvent()) });
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Can_Add_Multiple_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = Enumerable
      .Range(0, 10)
      .Select(_ => aggregate.Apply(new EmptyEvent()))
      .ToArray();
    await GetRecordStore().AddEventsAsync(events);
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Can_Add_Empty_List()
  {
    await GetRecordStore().AddEventsAsync(Array.Empty<Event<EmptyAggregate>>());
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await GetRecordStore().AddEventsAsync<EmptyAggregate>(null!));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Duplicate_Events()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());
    var e2 = aggregate.Apply(new EmptyEvent());

    e2 = e2 with { Index = 0 };

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e1, e2 }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Duplicate_Events_In_Separate_Calls()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());

    await GetRecordStore().AddEventsAsync(new [] { e1 });

    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 0 };

    await Assert.ThrowsAnyAsync<RecordStoreException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e2 }));
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Events_With_Different_AggregateIds()
  {
    var aggregate1 = new EmptyAggregate();
    var event1 = aggregate1.Apply(new EmptyEvent());

    var aggregate2 = new EmptyAggregate();
    var event2 = aggregate2.Apply(new EmptyEvent());

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { event1, event2 }));
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_NonConsecutive_Events()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());
    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 2 };

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e1, e2 }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_NonConsecutive_Events_In_Separate_Calls()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());

    await GetRecordStore().AddEventsAsync(new Event<EmptyAggregate>[] { e1 });
    
    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 2 };

    await Assert.ThrowsAnyAsync<RecordStoreException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e2 }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Event_With_Negative_Index()
  {
    var aggregate = new EmptyAggregate();
    var e = aggregate.Apply(new EmptyEvent()) with { Index = -1 };
    
    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Event_With_Null_Type()
  {
    var aggregate = new EmptyAggregate();
    var e = aggregate.Apply(new EmptyEvent()) with { Type = null! };
    
    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Event_With_Null_AggregateType()
  {
    var aggregate = new EmptyAggregate();
    var e = aggregate.Apply(new EmptyEvent()) with { AggregateType = null };
    
    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await GetRecordStore().AddEventsAsync(new [] { e }));
  }
  
  [Fact] // Tests issue https://github.com/Finaps/EventSourcing/issues/72
  public async Task RecordStore_AddEventsAsync_NonAlphabetical_Events_Persisted_In_Order()
  {
    var id = Guid.NewGuid();
    
    { // Create BankAccount and Persist 
      var bankAccount = new BankAccount { Id = id };
      var bankAccount2 = new BankAccount();

      bankAccount.Apply(new BankAccountCreatedEvent("E. Sourcing", "Some IBAN"));
      bankAccount2.Apply(new BankAccountCreatedEvent("Other Person", "Some other IBAN"));
    
      bankAccount.Apply(new BankAccountFundsDepositedEvent(500));
      bankAccount.Apply(new BankAccountFundsWithdrawnEvent(100));
      bankAccount.Apply(new BankAccountFundsTransferredEvent(50, bankAccount.Id, bankAccount2.Id));
      bankAccount.Apply(new BankAccountFundsWithdrawnEvent(20));
      bankAccount.Apply(new BankAccountFundsDepositedEvent(500));
      await GetAggregateService().PersistAsync(bankAccount);
    }

    { // Get Events and upload Modified ones

      var events = await GetRecordStore().GetEvents<BankAccount>()
        .Where(x => x.AggregateId == id)
        .OrderBy(x => x.Index)
        .AsAsyncEnumerable()
        .Cast<Event<BankAccount>>()
        .ToListAsync();

      var transaction = GetRecordStore().CreateTransaction();
      transaction.DeleteAllEvents<BankAccount>(id, events.Count - 1);
      transaction.AddEvents(events);
      await transaction.CommitAsync();
    }
  }
}