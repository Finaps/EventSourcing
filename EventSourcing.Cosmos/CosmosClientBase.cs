using System.Text.Json;
using EventSourcing.Core;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Client Base: Cosmos Connection for Querying and Storing <see cref="Event"/>s
/// </summary>
public abstract class CosmosClientBase<TEvent> where TEvent : Event
{
  private readonly CosmosClientOptions _clientOptions = new()
  {
    Serializer = new CosmosEventSerializer(new JsonSerializerOptions
    {
      Converters = { new EventConverter<TEvent>() }
    })
  };
  
  private protected readonly IOptions<CosmosEventStoreOptions> _options;
  private protected readonly Database _database;

  protected CosmosClientBase(IOptions<CosmosEventStoreOptions> options)
  {
    if (options?.Value == null)
      throw new ArgumentException("CosmosEventStoreOptions should not be null", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
      throw new ArgumentException("CosmosEventStoreOptions.ConnectionString should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Database))
      throw new ArgumentException("CosmosEventStoreOptions.Database should not be empty", nameof(options));

    _options = options;
    _database = new CosmosClient(options.Value.ConnectionString, _clientOptions)
      .GetDatabase(options.Value.Database);
  }
}