using EventSourcing.Core;
using EventSourcing.Core.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests
{
  public class CosmosViewStoreTests : ViewStoreTests
  {
    private readonly IOptions<CosmosStoreOptions> _viewStoreOptions;
    private readonly IOptions<CosmosStoreOptions> _eventStoreOptions;

    protected override IAggregateService GetAggregateService() =>
      new AggregateService(new CosmosEventStore(_eventStoreOptions));
    protected override IViewStore GetViewStore() => 
      new CosmosViewStore(GetAggregateService(), _viewStoreOptions);

    public CosmosViewStoreTests()
    {
      var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false)
        .AddJsonFile("appsettings.local.json", true)
        .AddEnvironmentVariables()
        .Build();

      _viewStoreOptions = Options.Create(new CosmosStoreOptions
      {
        ConnectionString = configuration["ViewStore:ConnectionString"],
        Database = configuration["ViewStore:Database"],
        Container = configuration["ViewStore:Container"]
      });
      
      _eventStoreOptions = Options.Create(new CosmosStoreOptions
      {
        ConnectionString = configuration["EventStore:ConnectionString"],
        Database = configuration["EventStore:Database"],
        Container = configuration["EventStore:Container"]
      });
    }
  }
}
