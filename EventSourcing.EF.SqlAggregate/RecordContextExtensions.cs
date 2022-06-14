using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.EF;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF.SqlAggregate;

public static class RecordContextExtensions
{
  public static IQueryable<TSqlAggregate> Aggregate<TAggregate, TSqlAggregate>(this RecordContext context)
    where TAggregate : Aggregate, new() where TSqlAggregate : SQLAggregate, new() => context.Set<TSqlAggregate>().FromSqlRaw(
$@"SELECT ({typeof(TAggregate).Name}{typeof(TSqlAggregate).Name}Aggregate(e ORDER BY ""{nameof(Event.Index)}"")).*
FROM ""{typeof(TAggregate).EventTable()}"" AS e
GROUP BY ""{nameof(SQLAggregate.AggregateId)}""");
}