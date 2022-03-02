using System.IO;
using EventSourcing.Core;
using Microsoft.Azure.Cosmos.Scripts;

namespace EventSourcing.Cosmos;

public static class CosmosStoredProcedures
{
    private const string BulkDeleteId = "DeleteAggregateAll";

    public static async Task CreateBulkDeleteProcedure(this Container container)
    {
        await TryDeleteStoredProcedure(container, BulkDeleteId);
        await CreateStoredProcedure(container, BulkDeleteId, StoredProcedures.DeleteAggregateAll);
    }
    public static async Task<int> ExecuteBulkDeleteProcedure(this Container container, Guid partitionId, Guid aggregateId)
    {
        var deleted = 0;
        StoredProcedureExecuteResponse<BulkDeleteResponse> response;
        
        do
        {
            response = await container.Scripts.ExecuteStoredProcedureAsync<BulkDeleteResponse>(BulkDeleteId,
                new PartitionKey($"{partitionId}"),
                new dynamic[] { container.Id, partitionId.ToString(), aggregateId.ToString() });
            deleted += response.Resource.deleted;
        } while (response.Resource.continuation);
        
        return deleted;
    }
    
    private static async Task CreateStoredProcedure(this Container container, string scriptId, string body) => 
        await container.Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties(scriptId, body));
    
    private static async Task TryDeleteStoredProcedure(this Container container, string sprocId)
    {
        var cosmosScripts = container.Scripts;

        try
        {
            var sproc = await cosmosScripts.ReadStoredProcedureAsync(sprocId);
            await cosmosScripts.DeleteStoredProcedureAsync(sprocId);
        }
        catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            //Nothing to delete
        }
    }
    private class BulkDeleteResponse
    {
        public int deleted { get; set; }
        public bool continuation { get; set; }
    }
}