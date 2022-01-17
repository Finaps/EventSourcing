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
        }
    }

    protected override Snapshot CreateSnapshot() =>
        new MockSnapshot { Counter = Counter };

    protected override void ApplySnapshot(Snapshot s)
    {
        if (s is not MockSnapshot snapshot)
            throw new InvalidOperationException();
        
        SnapshotsAppliedAfterHydration++;
        Counter = snapshot.Counter;
    }
}