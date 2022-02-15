using EventSourcing.Core;

namespace EventSourcing.Cosmos;

public class CosmosEventStoreOptions
{
  public string? ConnectionString { get; set; }
  public string? Database { get; set; }
  public string? Container { get; set; }

  public RecordConverterOptions? RecordConverterOptions { get; set; }
}