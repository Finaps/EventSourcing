namespace EventSourcing.Core;

public class AggregateTransaction : IAggregateTransaction
{
  private readonly IRecordTransaction _recordTransaction;
  private readonly HashSet<Aggregate> _aggregates = new();

  public AggregateTransaction(IRecordTransaction recordTransaction)
  {
    _recordTransaction = recordTransaction;
  }

  public IAggregateTransaction Add(Aggregate aggregate)
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate.Id cannot be empty", nameof(aggregate));

    if (!_aggregates.Add(aggregate))
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate already added.", nameof(aggregate));

    _recordTransaction.AddEvents(aggregate.UncommittedEvents.ToList());

    foreach (var snapshot in SnapshotService.GetSnapshots(aggregate))
      _recordTransaction.AddSnapshot(snapshot);

    foreach (var view in ViewService.GetViews(aggregate))
      _recordTransaction.AddView(view);

    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken)
  {
    await _recordTransaction.CommitAsync(cancellationToken);

    foreach (var aggregate in _aggregates)
      aggregate.ClearUncommittedEvents();
  }
}