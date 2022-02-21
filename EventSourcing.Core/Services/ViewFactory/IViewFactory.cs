namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="View"/> for <see cref="Aggregate"/>
/// </summary>
public interface IViewFactory
{
  Type AggregateType { get; }
  Type ViewType { get; }
  
  /// <summary>
  /// Create <see cref="View"/> for <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate"/></param>
  /// <returns>Resulting <see cref="View"/> of <see cref="Aggregate"/></returns>
  View CreateView(Aggregate aggregate);
}