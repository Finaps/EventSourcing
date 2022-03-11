using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests;

public class EntityFrameworkAggregateServiceTests : AggregateServiceTests
{
  protected override IRecordStore RecordStore =>
    new EntityFrameworkRecordStore(new TestContextFactory().CreateDbContext(Array.Empty<string>()));
  protected override IAggregateService AggregateService => new AggregateService(RecordStore);
}