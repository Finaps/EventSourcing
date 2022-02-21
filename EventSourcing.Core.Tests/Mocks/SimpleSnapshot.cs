  namespace EventSourcing.Core.Tests.Mocks;

public record SimpleSnapshot : Snapshot
{
    public int Counter { get; init; }
}