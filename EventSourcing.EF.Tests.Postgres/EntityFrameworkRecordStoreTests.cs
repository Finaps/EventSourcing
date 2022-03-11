using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests;

public class EntityFrameworkRecordStoreTests : RecordStoreTests
{
  protected override IRecordStore RecordStore =>
    new EntityFrameworkRecordStore(new TestContextFactory().CreateDbContext(Array.Empty<string>()));
}