using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests.SqlServer;

public class SqlServerEventSourcingTests : EventSourcingTests
{
  protected override IRecordStore RecordStore =>
    new EntityFrameworkRecordStore(new SqlServerTestContextFactory().CreateDbContext(Array.Empty<string>()));
}