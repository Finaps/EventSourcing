using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

internal static class DbContextExtensions
{
  public static async Task<int> DeleteWhereAsync(this RecordContext context,
    string table, Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) =>
    await context.DeleteWhereAsync(table, partitionId, aggregateId, null, cancellationToken);
  
  public static async Task<int> DeleteWhereAsync(this RecordContext context,
    string table, Guid partitionId, Guid aggregateId, long? index, CancellationToken cancellationToken = default)
  {
    var query = $"DELETE FROM \"{table}\" WHERE";
    query += $"\"{nameof(Event.PartitionId)}\" = '{partitionId}' ";
    query += $"AND \"{nameof(Event.AggregateId)}\" = '{aggregateId}'";
    
    if (index != null)
      query += $"AND \"{nameof(Event.Index)}\" = {index}";
    
    return await context.Database.ExecuteSqlRawAsync(query, cancellationToken);
  }
}