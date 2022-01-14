namespace EventSourcing.Core.Tests.Mocks;

public class SnapshotAggregate : Aggregate
{
    public override long SnapshotInterval => 10;

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
            case MockSnapshot snapshot:
                SnapshotsAppliedAfterHydration++;
                Counter = snapshot.Counter;
                break;
        }
    }

    protected override SnapshotEvent CreateSnapshot() =>
        new MockSnapshot { Counter = Counter };
}