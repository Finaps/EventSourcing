using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests.SqlServer;

public class SqlServerEventSourcingTests : EntityFrameworkEventSourcingTests
{
  public override RecordContext RecordContext => new SqlServerTestContextFactory().CreateDbContext(Array.Empty<string>());
  protected override IRecordStore RecordStore => new EntityFrameworkRecordStore(RecordContext);
}