namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  protected abstract IRecordStore RecordStore { get; }
  protected IAggregateService AggregateService => new AggregateService(RecordStore);
  protected IProjectionUpdateService ProjectionUpdateService => new ProjectionUpdateService(AggregateService, RecordStore);
}