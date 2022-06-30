using System.Reflection;
using Finaps.EventSourcing.Cosmos;

namespace Finaps.EventSourcing.Core;

internal sealed class RecordTypeCache
{
    private static readonly Dictionary<Type, string> RecordTypeStrings = Cache.RecordTypes
        .Append(typeof(CosmosRecordTransaction.CheckEvent))
        .Append(typeof(CosmosRecordTransaction.CheckSnapshot))
        .ToDictionary(
        type => type, type => type.GetCustomAttribute<RecordTypeAttribute>()?.Type ?? type.Name);

    private static readonly Dictionary<string, Type> RecordStringTypes = RecordTypeStrings.ToDictionary(
        x => x.Value, x => x.Key);

    private static readonly Dictionary<Type, PropertyInfo[]> NonNullableProperties = Cache.RecordTypes.ToDictionary(
        type => type, type => type.GetProperties()
            .Where(property => property.PropertyType.IsValueType 
                            && Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    
    public static Type GetRecordType(string typeString)
    {
        if (!RecordStringTypes.TryGetValue(typeString, out var type))
            throw new InvalidOperationException(
                $"Error getting record type for {typeString}. {typeString} not found in assembly. Ensure {typeString} extends {typeof(Record)}");

        return type;
    }
    
    public static string GetRecordTypeString(Type type)
    {
        if(!RecordTypeStrings.TryGetValue(type, out var typeString))
            throw new InvalidOperationException(
                $"Error getting record type string for {type}. {type} not found in assembly. Ensure {type.Name} extends {typeof(Record)}");

        return typeString;
    }
    public static IEnumerable<PropertyInfo> GetNonNullableProperties(Type type)
    {
        if(!NonNullableProperties.TryGetValue(type, out var properties))
            throw new InvalidOperationException($"Error getting non-nullable properties for {type}. {type} not found in assembly. Ensure {type.Name} extends {typeof(Record)}");

        return properties;
    }
}