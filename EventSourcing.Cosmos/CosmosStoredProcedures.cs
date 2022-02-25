using System.IO;
using EventSourcing.Core;
using Microsoft.Azure.Cosmos.Scripts;

namespace EventSourcing.Cosmos;

public static class CosmosStoredProcedures
{
    private const string BulkDeleteId = "BulkDelete";

    public static async Task CreateBulkDeleteProcedure(Container container)
    {
        await TryDeleteStoredProcedure(container, BulkDeleteId);
        var body = await File.ReadAllTextAsync(@"../../../../EventSourcing.Cosmos/StoredProcedures/BulkDelete.js");
        await CreateStoredProcedure(container, BulkDeleteId, body);
    }
    
    public static async Task<int> ExecuteBulkDeleteProcedure(Container container, Guid partitionId, Guid aggregateId)
    {
        var query = $"SELECT * FROM {container.Id} e WHERE e.AggregateId = '{aggregateId}' AND e.PartitionId = '{partitionId}' AND e.Kind = {(int) RecordKind.Event}";
        var response = await container.Scripts.ExecuteStoredProcedureAsync<BulkDeleteResponse>(BulkDeleteId, new PartitionKey($"{partitionId}"), new dynamic[] {query} );
        return response.Resource.deleted;
    }
    
    private static async Task CreateStoredProcedure(Container container, string scriptId, string body) => 
        await container.Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties(scriptId, body));
    
    private static async Task TryDeleteStoredProcedure(Container container, string sprocId)
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
    }
}