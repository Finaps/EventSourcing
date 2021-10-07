using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using EventSourcing.Cosmos.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventSourcing.Cosmos.Tests
{
  public class CosmosEventStoreTests
  {
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false)
        .AddJsonFile("appsettings.local.json", true)
        .Build();

    private static readonly IOptions<CosmosEventStoreOptions> CosmosOptions =
      Options.Create(new CosmosEventStoreOptions
      {
        ConnectionString = Configuration["Cosmos:ConnectionString"],
        Database = Configuration["Cosmos:Database"],
        Container = Configuration["Cosmos:Container"]
      });

    [Fact]
    public async Task Can_Add_Event()
    {
      var store = new CosmosEventStore(CosmosOptions);

      var aggregate = new MockAggregate();
      var @event = Event.Create<MockEvent>(aggregate);

      await store.AddAsync(new Event[] { @event });
    }
    
    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_Id()
    {
      var store = new CosmosEventStore(CosmosOptions);

      var aggregate = new MockAggregate();
      var @event = Event.Create<MockEvent>(aggregate);

      await store.AddAsync(new Event[] { @event });

      var exception = await Assert.ThrowsAsync<CosmosEventStoreException>(
        async () => await store.AddAsync(new Event[] { @event }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_Id_In_Batch()
    {
      var store = new CosmosEventStore(CosmosOptions);

      var aggregate = new MockAggregate();
      var @event = Event.Create<MockEvent>(aggregate);

      var exception = await Assert.ThrowsAsync<CosmosEventStoreException>(
        async () => await store.AddAsync(new Event[] { @event, @event }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }
    
    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version()
    {
      var store = new CosmosEventStore(CosmosOptions);

      var aggregate = new MockAggregate();
      var event1 = Event.Create<MockEvent>(aggregate);
      var event2 = Event.Create<MockEvent>(aggregate);

      await store.AddAsync(new Event[] { event1 });

      var exception = await Assert.ThrowsAsync<CosmosEventStoreException>(
        async () => await store.AddAsync(new Event[] { event2 }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }
    
    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version_In_Batch()
    {
      var store = new CosmosEventStore(CosmosOptions);

      var aggregate = new MockAggregate();
      var event1 = Event.Create<MockEvent>(aggregate);
      var event2 = Event.Create<MockEvent>(aggregate);

      var exception = await Assert.ThrowsAsync<CosmosEventStoreException>(
        async () => await store.AddAsync(new Event[] { event1, event2 }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }
    
    [Fact]
    public async Task Cannot_Add_Events_With_Different_AggregateIds_In_Batch()
    {
      var store = new CosmosEventStore(CosmosOptions);

      var aggregate1 = new MockAggregate();
      var event1 = Event.Create<MockEvent>(aggregate1);
      var aggregate2 = new MockAggregate();
      var event2 = Event.Create<MockEvent>(aggregate2);

      await Assert.ThrowsAsync<CosmosEventStoreException>(
        async () => await store.AddAsync(new Event[] { event1, event2 }));
    }
  }
}