 namespace EventSourcing.Core.Tests.Mocks;

public record MockSnapshot : Snapshot
{
    public int Counter { get; init; }
}