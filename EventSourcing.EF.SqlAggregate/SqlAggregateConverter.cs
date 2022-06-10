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
using NpgsqlTypes;

namespace EventSourcing.EF.SqlAggregate;

public class SqlAggregateConverter<TSqlAggregate> where TSqlAggregate : SqlAggregate, new()
{
  public readonly string EventTableName = GetAggregateType(typeof(TSqlAggregate)).EventTable();
  public readonly string AggregateTypeName = typeof(TSqlAggregate).Name;
  public string ApplyFunctionName => $"{AggregateTypeName}Apply";
  public string AggregateFunctionName => $"{AggregateTypeName}Aggregate";
  
  public string AggregateTypeDefinition => $"CREATE TYPE {AggregateTypeName} AS ({string.Join(", ", ConvertPropertyTypes())});";

  public string ApplyFunctionDefinition => 
    $"CREATE FUNCTION {ApplyFunctionName}({AggregateToken} {AggregateTypeName}, {EventToken} \"{EventTableName}\") " +
    $"RETURNS {AggregateTypeName}\n" +
    $"RETURN CASE\n{string.Join("\n", new TSqlAggregate().Clauses.Select(ConvertClause))}\nELSE {AggregateToken}\nEND;";

  public string AggregateFunctionDefinition =>
    $"CREATE AGGREGATE {AggregateFunctionName}(\"{EventTableName}\")\n" +
    "(\n" +
    $"  sfunc = {ApplyFunctionName},\n" +
    $"  stype = {AggregateTypeName},\n" +
    $"  initcond = '({string.Join(",", ConvertDefaultPropertyValues())})'\n" +
    ");";

  private static IEnumerable<PropertyInfo> Properties => typeof(TSqlAggregate).GetProperties().OrderBy(x => x.MetadataToken);
  private const string AggregateToken = "aggregate";
  private const string EventToken = "event";

  private static IEnumerable<string> ConvertPropertyTypes() => Properties
      .Select(x => $"{x.Name} {ConvertType(x.PropertyType)}");
  
  private static IEnumerable<string> ConvertDefaultPropertyValues() => Properties
    .Select(x => ConvertDefaultValue(x.PropertyType));

  private static string ConvertDefaultValue(Type type)
  {
    if (ConstructorTypeToSqlDefaultValue.TryGetValue(type, out var result))
      return result?.ToString() ?? "null";

    throw new NotSupportedException($"Type {type} is not supported");
  }
  
  private static string ConvertType(Type type)
  {
    if (ConstructorTypeToSqlType.TryGetValue(type, out var result))
      return result;

    throw new NotSupportedException($"Type {type} is not supported");
  }

  private static string ConvertClause(LambdaExpression expression) =>
    $"WHEN {EventToken}.\"{nameof(Event.Type)}\" = '{expression.Parameters.Last().Type.Name}' THEN {new SqlAggregateExpressionConverter().Convert(expression)}";

  private static Type GetAggregateType(Type? type)
  {
    while (type != null)
    {
      var aggregateType = type.GetGenericArguments().FirstOrDefault(typeof(Aggregate).IsAssignableFrom);
      if (aggregateType != null) return aggregateType;
      type = type.BaseType;
    }

    throw new InvalidOperationException("Couldn't find Aggregate Type");
  }

  // Adapted from: https://github.com/npgsql/npgsql/blob/main/src/Npgsql/TypeMapping/BuiltInTypeHandlerResolver.cs
  private static readonly Dictionary<Type, string> ConstructorTypeToSqlType = new()
  {
    // Numeric types
    { typeof(byte), "smallint" },
    { typeof(short), "smallint" },
    { typeof(int), "integer" },
    { typeof(long), "bigint" },
    { typeof(float), "real" },
    { typeof(double), "double precision" },
    { typeof(decimal), "decimal" },
    { typeof(BigInteger), "decimal" },

    // Text types
    { typeof(string), "text" },
    { typeof(char[]), "text" },
    { typeof(char), "text" },
    { typeof(ArraySegment<char>), "text" },
    { typeof(JsonDocument), "jsonb" },

    // Date/time types
    // The DateTime entry is for LegacyTimestampBehavior mode only. In regular mode we resolve through
    // ResolveValueDependentValue below
    { typeof(DateTime), "timestamp without time zone" },
    { typeof(DateTimeOffset), "timestamp with time zone" },
    { typeof(DateOnly), "date" },
    { typeof(TimeOnly), "time without time zone" },
    { typeof(TimeSpan), "interval" },
    { typeof(NpgsqlInterval), "interval" },

    // Network types
    { typeof(IPAddress), "inet" },
    // See ReadOnlyIPAddress below
    { typeof((IPAddress Address, int Subnet)), "inet" },
#pragma warning disable 618
    { typeof(NpgsqlInet), "inet" },
#pragma warning restore 618
    { typeof(PhysicalAddress), "macaddr" },

    // Full-text types
    { typeof(NpgsqlTsVector), "tsvector" },
    { typeof(NpgsqlTsQueryLexeme), "tsquery" },
    { typeof(NpgsqlTsQueryAnd), "tsquery" },
    { typeof(NpgsqlTsQueryOr), "tsquery" },
    { typeof(NpgsqlTsQueryNot), "tsquery" },
    { typeof(NpgsqlTsQueryEmpty), "tsquery" },
    { typeof(NpgsqlTsQueryFollowedBy), "tsquery" },

    // Geometry types
    { typeof(NpgsqlBox), "box" },
    { typeof(NpgsqlCircle), "circle" },
    { typeof(NpgsqlLine), "line" },
    { typeof(NpgsqlLSeg), "lseg" },
    { typeof(NpgsqlPath), "path" },
    { typeof(NpgsqlPoint), "point" },
    { typeof(NpgsqlPolygon), "polygon" },

    // Misc types
    { typeof(bool), "boolean" },
    { typeof(byte[]), "bytea" },
    { typeof(ArraySegment<byte>), "bytea" },
    { typeof(Guid), "uuid" },
    { typeof(BitArray), "bit varying" },
    { typeof(BitVector32), "bit varying" },
    { typeof(Dictionary<string, string>), "hstore" },

    // Internal types
    { typeof(NpgsqlLogSequenceNumber), "pg_lsn" },
    { typeof(NpgsqlTid), "tid" },
    { typeof(DBNull), "unknown" },

    // Built-in range types
    { typeof(NpgsqlRange<int>), "int4range" },
    { typeof(NpgsqlRange<long>), "int8range" },
    { typeof(NpgsqlRange<decimal>), "numrange" },
    { typeof(NpgsqlRange<DateOnly>), "daterange" },

    // Built-in multirange types
    { typeof(NpgsqlRange<int>[]), "int4multirange" },
    { typeof(List<NpgsqlRange<int>>), "int4multirange" },
    { typeof(NpgsqlRange<long>[]), "int8multirange" },
    { typeof(List<NpgsqlRange<long>>), "int8multirange" },
    { typeof(NpgsqlRange<decimal>[]), "nummultirange" },
    { typeof(List<NpgsqlRange<decimal>>), "nummultirange" },
    { typeof(NpgsqlRange<DateOnly>[]), "datemultirange" },
    { typeof(List<NpgsqlRange<DateOnly>>), "datemultirange" },
  };
  
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