using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Cosmos.Tests.Mocks;
using Microsoft.Extensions.Options;
using Xunit;

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

    [Fact]
    public async Task Can_Persist_And_Rehydrate_Aggregate()
    {
      var aggregate = new MockAggregate();
      
      aggregate.Add<MockEvent>(new
      {
        MockBoolean = true,
        MockInteger = 42,
        MockDouble = 3.14159265359,
        MockString = "Hello World"
      });
      
      aggregate.Add<MockEvent>(new
      {
        MockBoolean = false,
        MockInteger = 12,
        MockDouble = 2.71828182845904523,
        MockString = "Guten Tag"
      });

      var repository = new CosmosEventStore(RepositoryOptions);

      await repository.CreateIfNotExistsAsync();

      await repository.PersistAsync(aggregate);
      
      aggregate.Add<MockEvent>(new
      {
        MockBoolean = true,
        MockInteger = 7,
        MockDouble = 1.61803398875,
        MockString = "Bonjour"
      });

      await repository.PersistAsync(aggregate);

      var result = await repository.RehydrateAsync<MockAggregate>(aggregate.Id);
      
      Assert.Equal(aggregate.Id, result.Id);
    }

    [Fact]
    public async Task Can_Perform_Ridiculous_Calculation()
    {
      var repository = new CosmosEventStore<MockEvent>(RepositoryOptions);

      var result = await repository.Events
        .Where(x => !x.MockBoolean)
        .Select(x => x.MockDouble)
        .ToListAsync();
      
      Assert.NotEmpty(result);
    }
  }
}