using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Cosmos.Tests.Mocks;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventSourcing.Cosmos.Tests
{
  public class CosmosEventRepositoryTests
  {
    private static readonly IOptions<CosmosOptions> RepositoryOptions = Options.Create(
      new CosmosOptions {
        ConnectionString = "",
        Database = "Events",
        Container = "Events"
    });

    [Fact]
    public async Task Can_Append_And_Get_Events()
    {
      var repository = new CosmosEventRepository(RepositoryOptions);

      var aggregate = new MockAggregate();

      await repository.AppendAsync(new MockEvent(aggregate)
      {
        MockBoolean = true,
        MockInteger = 42,
        MockDouble = 3.14159265359,
        MockString = "Hello World"
      });
      
      await repository.AppendAsync(new MockEvent(aggregate)
        {
          MockBoolean = false,
          MockInteger = 12,
          MockDouble = 2.71828182845904523,
          MockString = "Guten Tag"
        }
      );

      var result = await repository.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .ToListAsync();
      
      Assert.Equal(2, result.Count);
      Assert.True(result.All(x => x.GetType() == typeof(MockEvent)));
      Assert.Single(result, x =>
        x is MockEvent { MockBoolean: true, MockInteger: 42, MockString: "Hello World" });
      Assert.Single(result, x =>
        x is MockEvent { MockBoolean: false, MockInteger: 12, MockString: "Guten Tag" });
    }

    [Fact]
    public async Task Can_Persist_And_Rehydrate_Aggregate()
    {
      var aggregate = new MockAggregate();
      
      aggregate.Add(new MockEvent(aggregate)
      {
        MockBoolean = true,
        MockInteger = 42,
        MockDouble = 3.14159265359,
        MockString = "Hello World"
      });
      
      aggregate.Add(new MockEvent(aggregate)
      {
        MockBoolean = false,
        MockInteger = 12,
        MockDouble = 2.71828182845904523,
        MockString = "Guten Tag"
      });

      var repository = new CosmosEventRepository(RepositoryOptions);

      await repository.PersistAsync(aggregate);
      
      aggregate.Add(new MockEvent(aggregate)
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
      var repository = new CosmosEventRepository<MockEvent>(RepositoryOptions);

      var result = await repository.Events
        .Where(x => !x.MockBoolean)
        .Select(x => x.MockDouble)
        .ToListAsync();
      
      Assert.NotEmpty(result);
    }
  }
}