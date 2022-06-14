using EventSourcing.EF.SqlAggregate;
using Finaps.EventSourcing.Core.Tests.Mocks;
using Finaps.EventSourcing.EF.SqlAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.Configuration;

namespace Finaps.EventSourcing.EF.Tests.Postgres;

public class PostgresTestRecordContext : EntityFrameworkTestRecordContext
{
  public PostgresTestRecordContext(DbContextOptions<PostgresTestRecordContext> options) : base(options) {}

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder
      .Aggregate<BankAccount, BankAccountSqlAggregate>()
      .Apply<BankAccountCreatedEvent>((a, e) =>
        new() { Name = e.Name, Iban = e.Iban })
      .Apply<BankAccountFundsDepositedEvent>((a, e) =>
        new() { Amount = a.Amount + e.Amount })
      .Apply<BankAccountFundsWithdrawnEvent>((a, e) =>
        new() { Amount = a.Amount - e.Amount })
      .Apply<BankAccountFundsTransferredEvent>((a, e) =>
        new() { Amount = a.Amount - (a.AggregateId == e.DebtorAccount ? -e.Amount : e.Amount) });
  }
}

// Needed for EF Core Migrations to spot the design time migration services
public class TestContextServices : MyDesignTimeServices, IDesignTimeServices { }

public class TestContextFactory : IDesignTimeDbContextFactory<PostgresTestRecordContext>
{
  public PostgresTestRecordContext CreateDbContext(string[] args) =>
    new (new DbContextOptionsBuilder<PostgresTestRecordContext>()
      .UseNpgsql(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("RecordStore"))
      .ReplaceService<IMigrationsModelDiffer, SqlAggregateMigrationsModelDiffer>()
      .ReplaceService<ICSharpMigrationOperationGenerator, SqlAggregateMigrationOperationGenerator>()
      .ReplaceService<IMigrationsSqlGenerator, SqlAggregateMigrationsSqlGenerator>()
      .UseAllCheckConstraints()
      .EnableSensitiveDataLogging()
      .Options);
}

