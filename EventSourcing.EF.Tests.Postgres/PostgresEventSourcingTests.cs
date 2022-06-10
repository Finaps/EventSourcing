using System;
using EventSourcing.EF.SqlAggregate;
using Finaps.EventSourcing.Core;
using Xunit;

namespace Finaps.EventSourcing.EF.Tests.Postgres;

public class PostgresEventSourcingTests : EntityFrameworkEventSourcingTests
{
  protected override IRecordStore RecordStore => new EntityFrameworkRecordStore(RecordContext);
  public override RecordContext RecordContext => new TestContextFactory().CreateDbContext(Array.Empty<string>());

  [Fact]
  public void Can_Get_Sql_Type_Definition()
  {
    var type = new SqlAggregateConverter<BankAccountSqlAggregate>();
    
    Assert.Equal("create type BankAccountSqlAggregate AS (PartitionId uuid);", type.AggregateTypeDefinition);
  }
}