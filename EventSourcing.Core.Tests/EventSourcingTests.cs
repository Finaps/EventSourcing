namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  protected abstract IRecordStore GetRecordStore();
  protected IAggregateService GetAggregateService() => new AggregateService(GetRecordStore());

  protected IProjectionUpdateService GetProjectionUpdateService() => 
    new ProjectionUpdateService(GetAggregateService(), GetRecordStore());
}