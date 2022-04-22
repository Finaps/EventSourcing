using EventSourcing.Core;
using Microsoft.Azure.Cosmos.Scripts;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Stored Procedures: Extension methods for the Cosmos Container to store and execute stored procedures
/// </summary>
internal static class CosmosStoredProcedures
{
  private const string DeleteAggregateAllScriptId = "DeleteAggregateAll";
  private const string DeleteAllEventsScriptId = "DeleteAllEvents";
  private const string DeleteAllSnapshotsScriptId = "DeleteAllSnapshots";

  public static async Task CreateDeleteAggregateAllProcedure(this Container container) =>
    await container.VerifyOrCreateStoredProcedure(DeleteAggregateAllScriptId, StoredProcedures.DeleteAggregateAll);
  public static async Task CreateDeleteAllEventsProcedure(this Container container) =>
    await container.VerifyOrCreateStoredProcedure(DeleteAllEventsScriptId, StoredProcedures.DeleteAllEvents);
  public static async Task CreateDeleteAllSnapshotsProcedure(this Container container) =>
    await container.VerifyOrCreateStoredProcedure(DeleteAllSnapshotsScriptId, StoredProcedures.DeleteAllSnapshots);
  public static async Task<int> DeleteAggregateAll(this Container container, Guid partitionId, Guid aggregateId) =>
    await container.ExecuteDeleteProcedure(
      DeleteAggregateAllScriptId,
      partitionId,
      new dynamic[] { container.Id, partitionId.ToString(), aggregateId.ToString() });
  public static async Task<int> DeleteAllEvents(this Container container, Guid partitionId, Guid aggregateId) =>
    await container.ExecuteDeleteProcedure(
      DeleteAllEventsScriptId,
      partitionId,
      new dynamic[] { container.Id, partitionId.ToString(), aggregateId.ToString(), (int) RecordKind.Event });
  public static async Task<int> DeleteAllSnapshots(this Container container, Guid partitionId, Guid aggregateId) =>
    await container.ExecuteDeleteProcedure(
      DeleteAllSnapshotsScriptId,
      partitionId,
      new dynamic[] { container.Id, partitionId.ToString(), aggregateId.ToString(), (int) RecordKind.Snapshot });
  
  
  
  private static async Task VerifyOrCreateStoredProcedure(this Container container, string scriptId, string script)
  {
    // Verify code currently stored for given script id and update only when needed
    if (await container.VerifyStoredProcedure(scriptId, script))
      return;
    
    await container.TryDeleteStoredProcedure(scriptId);
    await container.CreateStoredProcedure(scriptId, script);
  }
  
  private static async Task<int> ExecuteDeleteProcedure(this Container container, string deleteScriptId, Guid partitionId, dynamic[] parameters)
  {
    var deleted = 0;
    DeleteResponse response;

    do
    {
      response = (await container.Scripts.ExecuteStoredProcedureAsync<DeleteResponse>(
        deleteScriptId,
        new PartitionKey($"{partitionId}"),
        parameters)).Resource;
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

  private class DeleteResponse
  {
    public int deleted { get; set; }
    public bool continuation { get; set; }
  }
}