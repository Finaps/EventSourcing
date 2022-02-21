namespace EventSourcing.Core.Tests.Mocks;

public class SnapshotAggregate : Aggregate
{
  public int Counter;
  public int EventsAppliedAfterHydration;
  public int SnapshotsAppliedAfterHydration;

  protected override void Apply(Event e)
  {
    switch (e)
    {
      case EmptyEvent:
        EventsAppliedAfterHydration++;
        Counter++;
        break;
      case SimpleSnapshot snapshot:
        SnapshotsAppliedAfterHydration++;
        Counter = snapshot.Counter;
        break;
    }
  }
}

public class SimpleSnapshotFactory : SnapshotFactory<SnapshotAggregate, SimpleSnapshot>
{
  public override long SnapshotInterval => 10;
  protected override SimpleSnapshot CreateSnapshot(SnapshotAggregate aggregate) =>
    new() { Counter = aggregate.Counter };
}
