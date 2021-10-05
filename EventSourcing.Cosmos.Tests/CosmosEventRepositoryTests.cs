using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests
{
  public class CosmosEventRepositoryTests
  {
    private static readonly IOptions<ComosEventStoreOptions> RepositoryOptions = Options.Create(
      new ComosEventStoreOptions {
        ConnectionString = "",
        Database = "Events",
        Container = "Events"
    });
  }
}