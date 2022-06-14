using Finaps.EventSourcing.EF.SqlAggregate;

namespace Microsoft.EntityFrameworkCore.Migrations;

public static class MigrationBuilderExtensions
{
  public static void CreateSqlAggregate(this MigrationBuilder builder, string? assemblyQualifiedName) => 
    builder.Operations.Add(new AddSqlAggregateOperation(assemblyQualifiedName));
}