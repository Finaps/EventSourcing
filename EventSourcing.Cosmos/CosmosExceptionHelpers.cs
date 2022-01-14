using System.Reflection;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Cosmos;

internal static class CosmosExceptionHelpers
{
  public static void Throw(TransactionalBatchResponse response)
  {
    var inner = CreateCosmosException(response);

    throw response.StatusCode switch
    {
      HttpStatusCode.Conflict => new ConcurrencyException(inner),
      _ => new EventStoreException(response.StatusCode, inner)
    };
  }
  
  public static CosmosException CreateCosmosException(TransactionalBatchResponse response)
  {
    var subStatusCode = (int) response
      .GetType()
      .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
      .GetValue(response)!;
      
    return new CosmosException(response.ErrorMessage, response.StatusCode, subStatusCode, response.ActivityId, response.RequestCharge);
  }
}