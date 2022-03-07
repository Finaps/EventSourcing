using Microsoft.Azure.Cosmos.Scripts;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Stored Procedures: Extension methods for the Cosmos Container to store and execute stored procedures
/// </summary>
public static class CosmosStoredProcedures
{
  private const string BulkDeleteId = "DeleteAggregateAll";

  public static async Task CreateDeleteAggregateAllProcedure(this Container container)
  {
    // Verify code currently stored for DeleteAggregateAll procedure and update only when needed
    if (await container.VerifyStoredProcedure(BulkDeleteId, StoredProcedures.DeleteAggregateAll))
      return;

    await container.TryDeleteStoredProcedure(BulkDeleteId);
    await container.CreateStoredProcedure(BulkDeleteId, StoredProcedures.DeleteAggregateAll);
  }

  public static async Task<int> ExecuteDeleteAggregateAllProcedure(this Container container, Guid partitionId,
    Guid aggregateId)
  {
    var deleted = 0;
    DeleteAggregateAllResponse response;

    do
    {
      response = (await container.Scripts.ExecuteStoredProcedureAsync<DeleteAggregateAllResponse>(
        BulkDeleteId,
        new PartitionKey($"{partitionId}"),
        new dynamic[] { container.Id, partitionId.ToString(), aggregateId.ToString() })).Resource;
      deleted += response.deleted;
    } while (response.continuation);

    return deleted;
  }

  private static async Task CreateStoredProcedure(this Container container, string scriptId, string body) =>
    await container.Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties(scriptId, body));

  private static async Task<bool> VerifyStoredProcedure(this Container container, string id, string body)
  {
    try
    {
      var response = await container.Scripts.ReadStoredProcedureAsync(id);
      return response.Resource.Body == body;
    }
    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      return false;
    }
  }

  private static async Task TryDeleteStoredProcedure(this Container container, string id)
  {
    try
    {
      await container.Scripts.DeleteStoredProcedureAsync(id);
    }
    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      //Nothing to delete
    }
  }

  private class DeleteAggregateAllResponse
  {
    public int deleted { get; set; }
    public bool continuation { get; set; }
  }
}