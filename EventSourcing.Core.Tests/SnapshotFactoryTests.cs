namespace EventSourcing.Core.Tests.SnapshotFactoryTests;

public class SnapshotFactoryTests
{
  [Fact]
  public void Cannot_Create_Snapshot_For_Aggregate_Without_Events()
  {
    Assert.Throws<InvalidOperationException>(() =>
      new SimpleSnapshotFactory().CreateSnapshot(new SnapshotAggregate()));
  }
}