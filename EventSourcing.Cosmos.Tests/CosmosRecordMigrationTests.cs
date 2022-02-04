using EventSourcing.Core;
using EventSourcing.Core.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos.Tests;

public class CosmosRecordMigrationTests: RecordMigrationTests
{
    protected override IEventStore EventStore { get; }
    protected override ISnapshotStore SnapshotStore { get; }
    protected override IAggregateService AggregateService { get; }

    public CosmosRecordMigrationTests()
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
        SnapshotStore = new CosmosSnapshotStore(options);
        AggregateService = new AggregateService(EventStore, SnapshotStore, null);
    }
}