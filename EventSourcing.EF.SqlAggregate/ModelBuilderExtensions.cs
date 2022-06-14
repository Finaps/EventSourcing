using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF.SqlAggregate;

public static class ModelBuilderExtensions
{
  public static SqlAggregateBuilder<TAggregate, TSqlAggregate> Aggregate<TAggregate, TSqlAggregate>(this ModelBuilder builder)
    where TAggregate : Aggregate, new() where TSqlAggregate : SQLAggregate, new() => new(builder);
}