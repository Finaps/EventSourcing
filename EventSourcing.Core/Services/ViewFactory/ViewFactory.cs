namespace EventSourcing.Core;

public abstract class ViewFactory<TAggregate, TView> : IViewFactory where TAggregate : Aggregate where TView : View
{
  public Type AggregateType => typeof(TAggregate);
  public Type ViewType => typeof(TView);
  
  protected abstract TView CreateView(TAggregate aggregate);

  public View CreateView(Aggregate aggregate) => CreateView((TAggregate) aggregate) with
  {
    AggregateType = aggregate.Type,
    PartitionId = aggregate.PartitionId,
    Id = aggregate.Id,
    Version = aggregate.Version
  };
}