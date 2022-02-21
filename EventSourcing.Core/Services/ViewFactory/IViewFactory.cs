namespace EventSourcing.Core;

public interface IViewFactory
{
  Type AggregateType { get; }
  Type ViewType { get; }
  View CreateView(Aggregate aggregate);
}