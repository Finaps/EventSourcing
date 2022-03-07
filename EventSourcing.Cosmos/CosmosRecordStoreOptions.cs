using EventSourcing.Core;

namespace EventSourcing.Cosmos;

/// <summary>
/// <see cref="CosmosRecordStore"/> Options
/// </summary>
public class CosmosRecordStoreOptions
{
  /// <summary>
  /// Cosmos Connection String
  /// </summary>
  public string? ConnectionString { get; set; }
  
  /// <summary>
  /// Cosmos Database Name
  /// </summary>
  public string? Database { get; set; }
  
  /// <summary>
  /// Cosmos Container Name
  /// </summary>
  public string? Container { get; set; }

  /// <summary>
  /// <see cref="RecordConverter{TRecord}"/> Options
  /// </summary>
  public RecordConverterOptions? RecordConverterOptions { get; set; }
}