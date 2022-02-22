namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="TView"/> for <see cref="TAggregate"/>
/// </summary>
/// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
/// <typeparam name="TView"><see cref="View"/> type</typeparam>
public abstract class ViewFactory<TAggregate, TView> : IViewFactory where TAggregate : Aggregate where TView : View
{
  public Type AggregateType => typeof(TAggregate);
  public Type ViewType => typeof(TView);
  
  public View CreateView(Aggregate aggregate) => CreateView((TAggregate) aggregate) with
  {
    AggregateType = aggregate.Type,
    PartitionId = aggregate.PartitionId,
    AggregateId = aggregate.Id,
    Version = aggregate.Version
  };
  
  /// <summary>
  /// Create <see cref="TView"/> for <see cref="TAggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="TAggregate"/></param>
  /// <returns>Resulting <see cref="TView"/> of <see cref="TAggregate"/></returns>
  protected abstract TView CreateView(TAggregate aggregate);
}