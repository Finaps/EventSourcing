using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests;

public class PostgresEventSourcingTests : EventSourcingTests
{
  protected override IRecordStore RecordStore =>
    new EntityFrameworkRecordStore(new TestContextFactory().CreateDbContext(Array.Empty<string>()));
}