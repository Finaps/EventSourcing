using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace Finaps.EventSourcing.EF;

internal static class RecordContextExtensions
{
  public static async Task<int> DeleteWhereAsync(this RecordContext context,
    string table, Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var query = $@"
        DELETE FROM ""{table}""
        WHERE {SqlEquals(nameof(Event.PartitionId), partitionId)} AND 
              {SqlEquals(nameof(Event.AggregateId), aggregateId)}";

    return await context.Database.ExecuteSqlRawAsync(query, cancellationToken);
  }

  public static async Task<int> DeleteWhereAsync(this RecordContext context,
    string table, Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default)
  {
    var query = $@"
        DELETE FROM ""{table}"" WHERE
        {SqlEquals(nameof(Event.PartitionId), partitionId)} AND 
        {SqlEquals(nameof(Event.AggregateId), aggregateId)} AND 
        {SqlEquals(nameof(Event.Index), index)}";

    return await context.Database.ExecuteSqlRawAsync(query, cancellationToken);
  }

  private static string SqlEquals(string column, Guid id) => $"\"{column}\" = '{id}'";
  private static string SqlEquals(string column, long? index) => index == null ? "" : $"\"{column}\" = {index}";
}