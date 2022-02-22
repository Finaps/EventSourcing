namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
  protected abstract IRecordStore RecordStore { get; }
}