namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  protected abstract IRecordStore RecordStore { get; }
}