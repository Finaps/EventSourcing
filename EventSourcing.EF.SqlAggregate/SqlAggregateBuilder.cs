using System.Collections;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.EF;
using Finaps.EventSourcing.EF.SqlAggregate;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace EventSourcing.EF.SqlAggregate;


public abstract class SqlAggregateBuilder
{
  internal static Dictionary<Type, Dictionary<Type, SqlAggregateBuilder>> Cache { get; } = new();

  public abstract string SQL { get; }
}

public class SqlAggregateBuilder<TAggregate, TSqlAggregate> : SqlAggregateBuilder
  where TAggregate : Aggregate, new()
  where TSqlAggregate : SQLAggregate, new()
{
  public override string SQL => $"{ApplyFunctionDefinition}\n{AggregateFunctionDefinition}";
  
  private List<LambdaExpression> Clauses { get; } = new();
  
  private static string EventTableName => typeof(TAggregate).EventTable();
  private static string ApplyFunctionName => $"{typeof(TAggregate).Name}{typeof(TSqlAggregate).Name}Apply";
  private static string AggregateFunctionName => $"{typeof(TAggregate).Name}{typeof(TSqlAggregate).Name}Aggregate";
  private static string AggregateFunctionDefinition =>
    $"CREATE AGGREGATE {AggregateFunctionName}(\"{EventTableName}\")\n" +
    "(\n" +
    $"  sfunc = {ApplyFunctionName},\n" +
    $"  stype = \"{typeof(TSqlAggregate).Name}\",\n" +
    $"  initcond = '({string.Join(",", ConvertDefaultPropertyValues())})'\n" +
    ");";
  private string ApplyFunctionDefinition => 
    $"CREATE FUNCTION {ApplyFunctionName}({AggregateToken} \"{typeof(TSqlAggregate).Name}\", {EventToken} \"{EventTableName}\") " +
    $"RETURNS \"{typeof(TSqlAggregate).Name}\"\n" +
    $"RETURN CASE\n{string.Join("\n", Clauses.Select(ConvertClause))}\nELSE {AggregateToken}\nEND;";
  
  private static IEnumerable<PropertyInfo> Properties => typeof(TSqlAggregate).GetProperties().OrderBy(x => x.Name);
  private const string AggregateToken = "aggregate";
  private const string EventToken = "event";
  
  public SqlAggregateBuilder(ModelBuilder builder)
  {
    if (!Cache.ContainsKey(typeof(TSqlAggregate)))
      Cache.Add(typeof(TSqlAggregate), new Dictionary<Type, SqlAggregateBuilder>());

    Cache[typeof(TSqlAggregate)].TryAdd(typeof(TAggregate), this);
    
    builder.Entity<TSqlAggregate>()
      .HasKey(x => new { x.PartitionId, x.AggregateId });
    
    foreach (var (property, i) in typeof(TSqlAggregate).GetProperties().OrderBy(x => x.Name).Select((info, i) => (info, i)))
      builder.Entity<TSqlAggregate>()
        .Property(property.Name).HasColumnOrder(i);
  }

  public SqlAggregateBuilder<TAggregate, TSqlAggregate> Apply<TEvent>(
    Expression<Func<TSqlAggregate, TEvent, TSqlAggregate>> expression)
    where TEvent : Event<TAggregate>
  {
    Clauses.Add(expression);
    return this;
  }

  private static IEnumerable<string> ConvertDefaultPropertyValues() => Properties
    .Select(x => ConvertDefaultValue(x.PropertyType));

  private static string ConvertDefaultValue(Type type)
  {
    if (ConstructorTypeToSqlDefaultValue.TryGetValue(type, out var result))
      return result?.ToString() ?? "null";

    throw new NotSupportedException($"Type {type} is not supported");
  }

  private static string ConvertClause(LambdaExpression expression) =>
    $"WHEN {EventToken}.\"{nameof(Event.Type)}\" = '{expression.Parameters.Last().Type.Name}' THEN {new SqlAggregateExpressionConverter().Convert(expression)}";

  private static readonly Dictionary<Type, object?> ConstructorTypeToSqlDefaultValue = new()
  {
    // Numeric types
    { typeof(byte), default(byte) },
    { typeof(short), default(short) },
    { typeof(int), default(int) },
    { typeof(long), default(long) },
    { typeof(float), default(float) },
    { typeof(double), default(double) },
    { typeof(decimal), default(decimal) },
    { typeof(BigInteger), default(BigInteger) },

    // Text types
    { typeof(string), default(string) },
    { typeof(char[]), default(char[]) },
    { typeof(char), default(char[]) },
    { typeof(ArraySegment<char>), default(ArraySegment<char>) },
    { typeof(JsonDocument), default(JsonDocument) },

    // Date/time types
    // The DateTime entry is for LegacyTimestampBehavior mode only. In regular mode we resolve through
    // ResolveValueDependentValue below
    { typeof(DateTime), default(DateTime) },
    { typeof(DateTimeOffset), default(DateTimeOffset) },
    { typeof(DateOnly), default(DateOnly) },
    { typeof(TimeOnly), default(TimeOnly) },
    { typeof(TimeSpan), default(TimeSpan) },
    { typeof(NpgsqlInterval), default(NpgsqlInterval) },

    // Network types
    { typeof(IPAddress), default(IPAddress) },
    // See ReadOnlyIPAddress below
    { typeof((IPAddress Address, int Subnet)), default((IPAddress, int)) },
#pragma warning disable 618
    { typeof(NpgsqlInet), default(NpgsqlInet) },
#pragma warning restore 618
    { typeof(PhysicalAddress), default(PhysicalAddress) },

    // Full-text types
    { typeof(NpgsqlTsVector), default(NpgsqlTsVector) },
    { typeof(NpgsqlTsQueryLexeme), default(NpgsqlTsQueryLexeme) },
    { typeof(NpgsqlTsQueryAnd), default(NpgsqlTsQueryAnd) },
    { typeof(NpgsqlTsQueryOr), default(NpgsqlTsQueryOr) },
    { typeof(NpgsqlTsQueryNot), default(NpgsqlTsQueryNot) },
    { typeof(NpgsqlTsQueryEmpty), default(NpgsqlTsQueryEmpty) },
    { typeof(NpgsqlTsQueryFollowedBy), default(NpgsqlTsQueryFollowedBy) },

    // Geometry types
    { typeof(NpgsqlBox), default(NpgsqlBox) },
    { typeof(NpgsqlCircle), default(NpgsqlCircle) },
    { typeof(NpgsqlLine), default(NpgsqlLine) },
    { typeof(NpgsqlLSeg), default(NpgsqlLSeg) },
    { typeof(NpgsqlPath), default(NpgsqlPath) },
    { typeof(NpgsqlPoint), default(NpgsqlPoint) },
    { typeof(NpgsqlPolygon), default(NpgsqlPolygon) },

    // Misc types
    { typeof(bool), default(bool) },
    { typeof(byte[]), default(byte[]) },
    { typeof(ArraySegment<byte>), default(ArraySegment<byte>) },
    { typeof(Guid), default(Guid) },
    { typeof(BitArray), default(BitArray) },
    { typeof(BitVector32), default(BitVector32) },
    { typeof(Dictionary<string, string>), default(Dictionary<string, string>) },

    // Internal types
    { typeof(NpgsqlLogSequenceNumber), default(NpgsqlLogSequenceNumber) },
    { typeof(NpgsqlTid), default(NpgsqlTid) },
    { typeof(DBNull), default(DBNull) },

    // Built-in range types
    { typeof(NpgsqlRange<int>), default(NpgsqlRange<int>) },
    { typeof(NpgsqlRange<long>), default(NpgsqlRange<long>) },
    { typeof(NpgsqlRange<decimal>), default(NpgsqlRange<decimal>) },
    { typeof(NpgsqlRange<DateOnly>), default(NpgsqlRange<DateOnly>) },

    // Built-in multirange types
    { typeof(NpgsqlRange<int>[]), default(NpgsqlRange<int>[]) },
    { typeof(List<NpgsqlRange<int>>), default(List<NpgsqlRange<int>>) },
    { typeof(NpgsqlRange<long>[]), default(NpgsqlRange<long>[]) },
    { typeof(List<NpgsqlRange<long>>), default(List<NpgsqlRange<long>>) },
    { typeof(NpgsqlRange<decimal>[]), default(NpgsqlRange<decimal>[]) },
    { typeof(List<NpgsqlRange<decimal>>), default(List<NpgsqlRange<decimal>>) },
    { typeof(NpgsqlRange<DateOnly>[]), default(NpgsqlRange<DateOnly>[]) },
    { typeof(List<NpgsqlRange<DateOnly>>), default(List<NpgsqlRange<DateOnly>>) }
  };
}