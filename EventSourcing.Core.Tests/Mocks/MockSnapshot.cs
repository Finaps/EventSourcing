 namespace EventSourcing.Core.Tests.Mocks;

public record MockSnapshot : SnapshotEvent
{
    public int Counter { get; init; }
}