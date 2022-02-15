namespace EventSourcing.Core;

public class AggregateTransaction : IAggregateTransaction
{
  private readonly ITransaction _transaction;
  private readonly HashSet<Aggregate> _aggregates = new();

  public AggregateTransaction(ITransaction transaction)
  {
    _transaction = transaction;
  }

  public IAggregateTransaction Add(Aggregate aggregate)
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate.Id cannot be empty",
        nameof(aggregate));

    if (!_aggregates.Add(aggregate))
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate already added.",
        nameof(aggregate));

    _transaction.Add(aggregate.UncommittedEvents.ToList());

    if (aggregate.IsSnapshotIntervalExceeded())
      _transaction.Add(aggregate.CreateLinkedSnapshot());

    _transaction.Add(aggregate);

    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken)
  {
    await _transaction.CommitAsync(cancellationToken);

    foreach (var aggregate in _aggregates)
      aggregate.ClearUncommittedEvents();
  }
}