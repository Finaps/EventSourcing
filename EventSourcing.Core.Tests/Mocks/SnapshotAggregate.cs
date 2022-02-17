using EventSourcing.Core.Records;

namespace EventSourcing.Core.Tests.Mocks;

public record SnapshotAggregate : Aggregate
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
        new SimpleSnapshot { Counter = Counter };

    protected override void ApplySnapshot(Snapshot s)
    {
        if (s is not SimpleSnapshot snapshot)
            throw new InvalidOperationException();
        
        SnapshotsAppliedAfterHydration++;
        Counter = snapshot.Counter;
    }
}