using System.Linq.Expressions;
using Finaps.EventSourcing.Core;

namespace EventSourcing.EF.SqlAggregate;

public abstract class SqlAggregate
{
  internal abstract List<LambdaExpression> Clauses { get; }
  
  public Guid PartitionId { get; init; }
  public Guid AggregateId { get; init; }
  public long Version { get; init; }
}

public class SqlAggregate<TSqlAggregate, TAggregate> : SqlAggregate
  where TSqlAggregate : SqlAggregate, new()
  where TAggregate : Aggregate, new()
{
  internal override List<LambdaExpression> Clauses { get; } = new();

  protected void Apply<TEvent>(Expression<Func<TSqlAggregate, TEvent, TSqlAggregate>> expression)
    where TEvent : Event<TAggregate> => Clauses.Add(expression);
}