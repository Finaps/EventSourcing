using System.Reflection;
using EventSourcing.Core;

namespace EventSourcing.Cosmos;

internal static class CosmosExceptionHelpers
{
  public static void Throw(TransactionalBatchResponse response) =>
    throw new EventStoreException(response.StatusCode, CreateCosmosException(response));

  private static CosmosException CreateCosmosException(TransactionalBatchResponse response)
  {
    var subStatusCode = (int) response
      .GetType()
      .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
      .GetValue(response)!;
      
    return new CosmosException(response.ErrorMessage, response.StatusCode, subStatusCode, response.ActivityId, response.RequestCharge);
  }
}
