using System;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.EF.SqlAggregate;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finaps.EventSourcing.EF.Tests.Postgres;

public class PostgresEventSourcingTests : EntityFrameworkEventSourcingTests
{
  protected override IRecordStore RecordStore => new EntityFrameworkRecordStore(RecordContext);
  public override RecordContext RecordContext => new TestContextFactory().CreateDbContext(Array.Empty<string>());

  [Fact]
  public async Task Can_Aggregate_Sql_Aggregate()
  {
    var result = await RecordContext
      .Aggregate<BankAccount, BankAccountSqlAggregate>()
      .Where(x => x.Amount > 50)
      .ToListAsync();
  }
}