using System.Reflection;
using System.Text.Json;
using EventSourcing.Core;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Client Base: Cosmos Connection for Querying and Storing <see cref="Event"/>s
/// </summary>
public abstract class CosmosRecordStore<TRecord> where TRecord : Record
{
  private protected readonly Database Database;

  protected CosmosRecordStore(IOptions<CosmosEventStoreOptions> options)
  {
    if (options.Value == null)
      throw new ArgumentException("CosmosEventStoreOptions should not be null", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
      throw new ArgumentException("CosmosEventStoreOptions.ConnectionString should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Database))
      throw new ArgumentException("CosmosEventStoreOptions.Database should not be empty", nameof(options));
    
    Database = new CosmosClient(options.Value.ConnectionString, new CosmosClientOptions
    {
      Serializer = new CosmosRecordSerializer(new JsonSerializerOptions
      {
        Converters = { new RecordConverter(options.Value?.RecordConverterOptions) }
      })
    }).GetDatabase(options.Value!.Database);
  }
  
  protected static CosmosException CreateCosmosException(TransactionalBatchResponse response)
  {
    var subStatusCode = (int) response
      .GetType()
      .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
      .GetValue(response)!;
      
    return new CosmosException(response.ErrorMessage, response.StatusCode, subStatusCode, response.ActivityId, response.RequestCharge);
  }
}