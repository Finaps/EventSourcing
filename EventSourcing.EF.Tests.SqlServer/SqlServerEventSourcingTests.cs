using System;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF.Tests.SqlServer;

public class SqlServerEventSourcingTests : EntityFrameworkEventSourcingTests
{
  public override RecordContext RecordContext => new SqlServerTestContextFactory().CreateDbContext(Array.Empty<string>());
  protected override IRecordStore RecordStore => new EntityFrameworkRecordStore(RecordContext);
}