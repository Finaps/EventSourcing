using System;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventSourcing.Cosmos.Tests;

public class CosmosSnapshotStoreTests : SnapshotStoreTests
{
    private readonly IOptions<CosmosEventStoreOptions> _options;
    protected override ISnapshotStore GetSnapshotStore() =>
        new CosmosSnapshotStore(_options);

    protected override ISnapshotStore<TBaseEvent> GetSnapshotStore<TBaseEvent>() =>
        new CosmosSnapshotStore<TBaseEvent>(_options);
        
    public CosmosSnapshotStoreTests()
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
            SnapshotsContainer = configuration["Cosmos:SnapshotsContainer"]
        });
    }
        
    [Fact]
    public async Task Throws_ArgumentException_With_Missing_Container_Name()
    {
        Assert.Throws<ArgumentException>(() => new CosmosEventStore(Options.Create(new CosmosEventStoreOptions
        {
            ConnectionString = "A", Database = "B", SnapshotsContainer = ""
        })));
    }
}