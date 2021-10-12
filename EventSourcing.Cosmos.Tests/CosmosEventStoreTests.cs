using EventSourcing.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests
{
  public class CosmosEventStoreTests : EventStoreTests
  {
    public override IEventStore Store { get; }

    public CosmosEventStoreTests()
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

      Store = new CosmosEventStore(options);
    }
  }
}