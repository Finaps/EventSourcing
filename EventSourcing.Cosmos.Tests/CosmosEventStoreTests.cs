using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Records;
using EventSourcing.Core.Services;
using EventSourcing.Core.Tests;
using EventSourcing.Core.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventSourcing.Cosmos.Tests;

public class CosmosEventStoreTests : EventStoreTests
{
  private readonly IOptions<CosmosEventStoreOptions> _options;

  protected override IEventStore EventStore { get; }

  public CosmosEventStoreTests()
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", false)
      .AddJsonFile("appsettings.local.json", true)
      .AddEnvironmentVariables()
      .Build();

    _options = Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = configuration["Cosmos:ConnectionString"],
      Database = configuration["Cosmos:Database"],
      Container = configuration["Cosmos:Container"]
    });

    EventStore = new CosmosEventStore(_options);
  }

  [Fact]
  public Task Throws_ArgumentException_With_Missing_ConnectionString()
  {
    Assert.Throws<ArgumentException>(() => new CosmosEventStore(Options.Create(new CosmosEventStoreOptions {
      ConnectionString = " ", Database = "A", Container = "B"
    })));
    
    return Task.CompletedTask;
  }
    
  [Fact]
  public Task Throws_ArgumentException_With_Missing_Database_Name()
  {
    Assert.Throws<ArgumentException>(() => new CosmosEventStore(Options.Create(new CosmosEventStoreOptions {
      ConnectionString = "A", Database = null, Container = "B"
    })));
    
    return Task.CompletedTask;
  }
    
        
  [Fact]
  public Task Throws_ArgumentException_With_Missing_Container_Name()
  {
    Assert.Throws<ArgumentException>(() => new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = "A", Database = "B", Container = ""
    })));
    
    return Task.CompletedTask;
  }

  [Fact]
  public async Task Throws_Unauthorized_When_Adding_Event_With_Invalid_Options()
  {
    var store = new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      // Invalid Connection String
      ConnectionString = "AccountEndpoint=https://example.documents.azure.com:443/;AccountKey=JKnJg81PiP0kkqhCu0k3mKlEPEEBqlFxwM4eiyd3WX2HKUYAAglbc9vMRJQhDsUomD3VHpwrWO9O5nL4ENwLFw==;",
      Database = _options.Value.Database,
      Container =_options.Value.Container
    }));

    var exception = await Assert.ThrowsAsync<EventStoreException>(async () =>
      await store.AddAsync(new List<Event> { new EmptyAggregate().Add(new EmptyEvent()) }));
    Assert.Contains("401", exception.Message);
  }
    
  [Fact]
  public async Task Throws_NotFound_When_Adding_Event_With_NonExistent_Database()
  {
    var store = new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = _options.Value.ConnectionString,
      Database = "Invalid",
      Container = _options.Value.Container
    }));

    var exception = await Assert.ThrowsAsync<EventStoreException>(async () =>
      await store.AddAsync(new List<Event> { new EmptyAggregate().Add(new EmptyEvent()) }));
    Assert.Contains("404", exception.Message);
  }
    
  [Fact]
  public async Task Throws_NotFound_When_Adding_Event_With_Invalid_Container()
  {
    var store = new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = _options.Value.ConnectionString,
      Database = _options.Value.Database,
      Container = "Invalid"
    }));

    var exception = await Assert.ThrowsAsync<EventStoreException>(async () =>
      await store.AddAsync(new List<Event> { new EmptyAggregate().Add(new EmptyEvent()) }));
    Assert.Contains("404", exception.Message);
  }
    
  [Fact]
  public async Task Throws_Unauthorized_When_Querying_Events_With_Invalid_Options()
  {
    var store = new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      // Invalid Connection String
      ConnectionString = "AccountEndpoint=https://example.documents.azure.com:443/;AccountKey=JKnJg81PiP0kkqhCu0k3mKlEPEEBqlFxwM4eiyd3WX2HKUYAAglbc9vMRJQhDsUomD3VHpwrWO9O5nL4ENwLFw==;",
      Database = _options.Value.Database,
      Container =_options.Value.Container
    }));
      
    var exception = await Assert.ThrowsAsync<EventStoreException>(async () =>
      await store.Events
        .Where(x => x.AggregateId == Guid.NewGuid())
        .AsAsyncEnumerable()
        .ToListAsync()
      );
    Assert.Contains("401", exception.Message);
  }
    
  [Fact]
  public async Task Throws_NotFound_When_Querying_Events_With_NonExistent_Database()
  {
    var store = new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = _options.Value.ConnectionString,
      Database = "Invalid",
      Container = _options.Value.Container
    }));

    var exception = await Assert.ThrowsAsync<EventStoreException>(async () =>
      await store.Events
        .Where(x => x.AggregateId == Guid.NewGuid())
        .AsAsyncEnumerable()
        .ToListAsync()
      );
    Assert.Contains("404", exception.Message);
  }
    
  [Fact]
  public async Task Throws_NotFound_When_Querying_Events_With_NonExistent_Container()
  {
    var store = new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = _options.Value.ConnectionString,
      Database = _options.Value.Database,
      Container = "Invalid"
    }));

    var exception = await Assert.ThrowsAsync<EventStoreException>(async () =>
      await store.Events
        .Where(x => x.AggregateId == Guid.NewGuid())
        .AsAsyncEnumerable()
        .ToListAsync()
      );
    Assert.Contains("404", exception.Message);
  }
}