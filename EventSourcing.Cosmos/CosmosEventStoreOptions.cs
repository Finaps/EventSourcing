using EventSourcing.Core;

namespace EventSourcing.Cosmos;

public class CosmosEventStoreOptions
{
  public string ConnectionString { get; set; }
  public string Database { get; set; }
  public string EventsContainer { get; set; }
  public string SnapshotsContainer { get; set; }
  
  public RecordConverterOptions? RecordConverterOptions { get; set; }
}