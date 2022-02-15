using EventSourcing.Core;
using EventSourcing.Core.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests;

public class CosmosAggregateServiceTests : AggregateServiceTests
{
  protected sealed override IEventStore EventStore { get; }
  protected sealed override IAggregateService AggregateService { get; }

  public CosmosAggregateServiceTests()
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", false)
      .AddJsonFile("appsettings.local.json", true)
      .AddEnvironmentVariables()
      .Build();

    var options = Options.Create(new CosmosEventStoreOptions
    {
      ConnectionString = configuration["Cosmos:ConnectionString"],
      Database = configuration["Cosmos:Database"],
      Container = configuration["Cosmos:Container"]
    });

    EventStore = new CosmosEventStore(options);
    AggregateService = new AggregateService(EventStore);
  }
}