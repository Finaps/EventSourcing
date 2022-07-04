using System.Threading;

namespace Finaps.EventSourcing.Core.Tests.Mocks;

public class MockAggregateTransactionSubclass : AggregateTransaction
{
  public byte AddAggregateAsyncCallCount { get; private set; }
  public byte AddEventsAsyncCallCount { get; private set; }
  public byte AddSnapshotAsyncCallCount { get; private set; }
  public byte UpsertProjectionAsyncCallCount { get; private set; }
  public byte CommitAsyncCallCount { get; private set; }
  
  public MockAggregateTransactionSubclass(IRecordTransaction recordTransaction) : base(recordTransaction) { }

  public override Task<IAggregateTransaction> AddAggregateAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
  {
    AddAggregateAsyncCallCount++;
    return base.AddAggregateAsync(aggregate, cancellationToken);
  }

  protected override Task AddEventsAsync<TAggregate>(List<Event<TAggregate>> events, CancellationToken cancellationToken = default)
  {
    AddEventsAsyncCallCount++;
    return base.AddEventsAsync(events, cancellationToken);
  }

  protected override Task AddSnapshotAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken = default)
  {
    AddSnapshotAsyncCallCount++;
    return base.AddSnapshotAsync(snapshot, cancellationToken);
  }

  protected override Task UpsertProjectionAsync(Projection projection, CancellationToken cancellationToken = default)
  {
    UpsertProjectionAsyncCallCount++;
    return base.UpsertProjectionAsync(projection, cancellationToken);
  }

  public override Task CommitAsync(CancellationToken cancellationToken = default)
  {
    CommitAsyncCallCount++;
    return base.CommitAsync(cancellationToken);
  }
}