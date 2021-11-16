using System;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos
{
  public abstract class CosmosStore
  {
    protected Container Container { get; }
    
    protected CosmosStore(IOptions<CosmosStoreOptions> options, CosmosClientOptions clientOptions)
    {
      if (options?.Value == null)
        throw new ArgumentException("CosmosEventStoreOptions should not be null", nameof(options));
      
      if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        throw new ArgumentException("CosmosEventStoreOptions.ConnectionString should not be empty", nameof(options));
      
      if (string.IsNullOrWhiteSpace(options.Value.Database))
        throw new ArgumentException("CosmosEventStoreOptions.Database should not be empty", nameof(options));
      
      if (string.IsNullOrWhiteSpace(options.Value.Container))
        throw new ArgumentException("CosmosEventStoreOptions.Container should not be empty", nameof(options));
      
      Container = new CosmosClient(options.Value.ConnectionString, clientOptions)
        .GetDatabase(options.Value.Database).GetContainer(options.Value.Container);
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
}