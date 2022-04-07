using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests.Postgres;

public class PostgresEventSourcingTests : EntityFrameworkEventSourcingTests
{
  protected override IRecordStore RecordStore => new EntityFrameworkRecordStore(RecordContext);
  public override RecordContext RecordContext => new TestContextFactory().CreateDbContext(Array.Empty<string>());
}