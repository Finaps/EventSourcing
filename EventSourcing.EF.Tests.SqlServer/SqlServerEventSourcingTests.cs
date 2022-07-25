using System;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF.Tests.SqlServer;

public class SqlServerEventSourcingTests : EntityFrameworkEventSourcingTests
{
  public override RecordContext GetRecordContext() =>
    new SqlServerTestContextFactory().CreateDbContext(Array.Empty<string>());

  protected override IRecordStore GetRecordStore() => new EntityFrameworkRecordStore(GetRecordContext());
}