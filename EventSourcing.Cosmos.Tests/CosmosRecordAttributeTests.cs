using EventSourcing.Core;
using EventSourcing.Core.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests;

public class CosmosRecordAttributeTests: RecordAttributeTests
{
    protected override IEventStore EventStore { get; }

    public CosmosRecordAttributeTests()
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
            EventsContainer = configuration["Cosmos:EventsContainer"],
            SnapshotsContainer = configuration["Cosmos:SnapshotsContainer"]
        });

        EventStore = new CosmosEventStore(options);
    }
}