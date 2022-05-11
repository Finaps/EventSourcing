using System;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF.Tests.Postgres;

public class PostgresEventSourcingTests : EntityFrameworkEventSourcingTests
{
  protected override IRecordStore RecordStore => new EntityFrameworkRecordStore(RecordContext);
  public override RecordContext RecordContext => new TestContextFactory().CreateDbContext(Array.Empty<string>());
}