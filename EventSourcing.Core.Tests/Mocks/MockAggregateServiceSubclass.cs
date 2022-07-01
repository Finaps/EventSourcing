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

  public override Task<IAggregateTransaction> AddAggregateAsync(Aggregate aggregate)
  {
    AddAggregateAsyncCallCount++;
    return base.AddAggregateAsync(aggregate);
  }

  protected override Task AddEventsAsync(List<Event> events)
  {
    AddEventsAsyncCallCount++;
    return base.AddEventsAsync(events);
  }

  protected override Task AddSnapshotAsync(Snapshot snapshot)
  {
    AddSnapshotAsyncCallCount++;
    return base.AddSnapshotAsync(snapshot);
  }

  protected override Task UpsertProjectionAsync(Projection projection)
  {
    UpsertProjectionAsyncCallCount++;
    return base.UpsertProjectionAsync(projection);
  }

  public override Task CommitAsync(CancellationToken cancellationToken = default)
  {
    CommitAsyncCallCount++;
    return base.CommitAsync(cancellationToken);
  }
}