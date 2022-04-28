using System.Reflection;

namespace Finaps.EventSourcing.Cosmos;

internal static class CosmosExceptionHelpers
{
  public static CosmosException CreateCosmosException(TransactionalBatchResponse response)
  {
    var subStatusCode = (int) response
      .GetType()
      .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
      .GetValue(response)!;
      
    return new CosmosException(response.ErrorMessage, response.StatusCode, subStatusCode, response.ActivityId, response.RequestCharge);
  }
}
