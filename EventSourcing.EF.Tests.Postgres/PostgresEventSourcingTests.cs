using System;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF.Tests.Postgres;

public class PostgresEventSourcingTests : EntityFrameworkEventSourcingTests
{
  protected override IRecordStore GetRecordStore() => new EntityFrameworkRecordStore(GetRecordContext());
  public override RecordContext GetRecordContext() => new TestContextFactory().CreateDbContext(Array.Empty<string>());
}