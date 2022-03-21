namespace EventSourcing.Core.Tests.Mocks;

public record SnapshotEvent : Event<SnapshotAggregate>;

public record SnapshotSnapshot : Snapshot<SnapshotAggregate>
{
  public int Counter { get; init; }
}

public class SnapshotAggregate : Aggregate<SnapshotAggregate>
{
  public int Counter;
  public int EventsAppliedAfterHydration;
  public int SnapshotsAppliedAfterHydration;

  protected override void Apply(Event<SnapshotAggregate> e)
  {
    switch (e)
    {
      case SnapshotEvent:
        EventsAppliedAfterHydration++;
        Counter++;
        break;
    }
  }

  protected override void Apply(Snapshot<SnapshotAggregate> s)
  {
    switch (s)
    {
      case SnapshotSnapshot snapshot:
        SnapshotsAppliedAfterHydration++;
        Counter = snapshot.Counter;
        break;
    }
  }
}

public class SimpleSnapshotFactory : SnapshotFactory<SnapshotAggregate, SnapshotSnapshot>
{
  public override long SnapshotInterval => 10;
  protected override SnapshotSnapshot CreateSnapshot(SnapshotAggregate aggregate) =>
    new() { Counter = aggregate.Counter };
}
