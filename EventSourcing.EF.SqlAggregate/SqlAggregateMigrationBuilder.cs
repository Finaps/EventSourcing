using EventSourcing.EF.SqlAggregate;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Finaps.EventSourcing.EF.SqlAggregate;

public class AddSqlAggregateOperation : MigrationOperation
{
  public string SQL { get; }

  public AddSqlAggregateOperation(string sql) => SQL = sql;
}

public class SqlAggregateMigrationsModelDiffer : MigrationsModelDiffer
{
  public SqlAggregateMigrationsModelDiffer(IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotations, IChangeDetector changeDetector, IUpdateAdapterFactory updateAdapterFactory, CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies) { }
  
  protected override IEnumerable<MigrationOperation> Add(ITable target, DiffContext diffContext)
  {
    var type = target.EntityTypeMappings.First().EntityType.ClrType;
    
    var operations = base.Add(target, diffContext).ToList();

    if (type.IsSubclassOf(typeof(SQLAggregate)) && SqlAggregateBuilder.Cache.TryGetValue(type, out var builders))
      operations.AddRange(builders.Values.Select(b => new AddSqlAggregateOperation(b.SQL)));
    
    return operations;
  }
}

public class SqlAggregateMigrationOperationGenerator : CSharpMigrationOperationGenerator
{
  public SqlAggregateMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies) : base(dependencies) { }
  
  protected override void Generate(MigrationOperation operation, IndentedStringBuilder builder)
  {
    if (operation is AddSqlAggregateOperation op)
      builder.Append(@$".{nameof(MigrationBuilderExtensions.CreateSqlAggregate)}({
        Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(op.SQL, true)})");
  }
}

public class SqlAggregateMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
  public SqlAggregateMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, INpgsqlOptions npgsqlOptions) : base(dependencies, npgsqlOptions) { }

  protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
  {
    if (operation is AddSqlAggregateOperation op)
      builder.AppendLine(op.SQL).EndCommand();
    else
      base.Generate(operation, model, builder);
  }
}

public class MyDesignTimeServices : IDesignTimeServices
{
  public void ConfigureDesignTimeServices(IServiceCollection services)
  {
    services
      .AddSingleton<IMigrationsModelDiffer, SqlAggregateMigrationsModelDiffer>()
      .AddSingleton<ICSharpMigrationOperationGenerator, SqlAggregateMigrationOperationGenerator>()
      .AddSingleton<IMigrationsSqlGenerator, SqlAggregateMigrationsSqlGenerator>();
  }
}