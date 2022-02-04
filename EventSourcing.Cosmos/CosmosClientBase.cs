using System.Text.Json;
using EventSourcing.Core;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Client Base: Cosmos Connection for Querying and Storing <see cref="Event"/>s
/// </summary>
public abstract class CosmosClientBase<TRecord> where TRecord : Record
{
  private protected readonly Database Database;

  protected CosmosClientBase(IOptions<CosmosEventStoreOptions> options)
  {
    if (options?.Value == null)
      throw new ArgumentException("CosmosEventStoreOptions should not be null", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
      throw new ArgumentException("CosmosEventStoreOptions.ConnectionString should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Database))
      throw new ArgumentException("CosmosEventStoreOptions.Database should not be empty", nameof(options));
    
    Database = new CosmosClient(options.Value.ConnectionString, new CosmosClientOptions
      {
        Serializer = new CosmosEventSerializer(new JsonSerializerOptions
        {
          Converters = { new RecordConverter<TRecord>(options.Value?.RecordConverterOptions) }
        })
      })
      .GetDatabase(options.Value.Database);
  }
}