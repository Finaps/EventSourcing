using EventSourcing.Core;
using EventSourcing.Core.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests
{
  public class CosmosEventStoreTests : EventStoreTests
  {
    private readonly IOptions<CosmosEventStoreOptions> _options;
    
    public override IEventStore GetEventStore() =>
      new CosmosEventStore(_options);

    public override IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() =>
      new CosmosEventStore<TBaseEvent>(_options);

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
    }
  }
}