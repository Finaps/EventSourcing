using System.Reflection;

namespace EventSourcing.Core.Types;

public sealed class RecordTypesCache
{
    // Static RecordTypes cache
    private static readonly List<Type> AssemblyRecordTypes = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
        .ToList();
    private static readonly Dictionary<string, Type> RecordTypes;
    private static readonly Dictionary<Type, string> RecordTypesRev;
    private static readonly Dictionary<Type, PropertyInfo[]> NonNullableRecordProperties;
    public static Type GetRecordTypeStatic(string typeString) => GetRecordType(RecordTypes, typeString);
    public static string GetRecordTypeStringStatic(Type type) => GetRecordTypeString(RecordTypesRev, type);
    public static PropertyInfo[] GetNonnullablePropertiesStatic(Type type) => GetNonnullableRecordProperties(NonNullableRecordProperties, type);
    static RecordTypesCache()
    {
        // Create dictionaries mapping from Record.Type string to Record Type and it's reverse
        RecordTypes = AssemblyRecordTypes.ToDictionary(type => type.GetCustomAttribute<RecordType>()?.Value ?? type.Name);
        RecordTypesRev = RecordTypes.ToDictionary(kv => kv.Value, kv => kv.Key);
        
        // For each Record Type, create set of non-nullable properties for validation
        NonNullableRecordProperties = RecordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
            .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    }
    
    // Non-static RecordTypes cache
    private readonly Dictionary<string, Type> _recordTypes;
    private readonly Dictionary<Type, string> _recordTypesRev;
    private readonly Dictionary<Type, PropertyInfo[]> _nonNullableRecordProperties;
    public Type GetRecordType(string typeString) => GetRecordType(_recordTypes, typeString);
    public string GetRecordTypeString(Type type) => GetRecordTypeString(_recordTypesRev, type);
    public PropertyInfo[] GetNonnullableProperties(Type type) => GetNonnullableRecordProperties(_nonNullableRecordProperties, type);

    public RecordTypesCache(List<Type>? recordTypes)
    {
        if (recordTypes == null)
        {
            _recordTypes = RecordTypes;
            _recordTypesRev = RecordTypesRev;
            _nonNullableRecordProperties = NonNullableRecordProperties;
            return;
        }
        // Create dictionaries mapping from Record.Type string to Record Type and it's reverse
        _recordTypes = recordTypes.ToDictionary(type => type.GetCustomAttribute<RecordType>()?.Value ?? type.Name);
        _recordTypesRev = _recordTypes.ToDictionary(kv => kv.Value, kv => kv.Key);
        
        // For each Record Type, create set of non-nullable properties for validation
        _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
            .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    }

    
    
    private static Type GetRecordType(Dictionary<string, Type> recordTypes, string typeString)
    {
        if (!recordTypes.TryGetValue(typeString, out var type))
            throw new InvalidOperationException($"Record with Type '{typeString}' not found");

        return type;
    }
    
    private static string GetRecordTypeString(Dictionary<Type, string> recordTypesRev ,Type type)
    {
        if(!recordTypesRev.TryGetValue(type, out var typeString))
            throw new InvalidOperationException($"Record type for Type '{type.Name}' not found");

        return typeString;
    }
    
    private static PropertyInfo[] GetNonnullableRecordProperties(Dictionary<Type, PropertyInfo[]> nonnullableProperties, Type type)
    {
        if(!nonnullableProperties.TryGetValue(type, out var properties))
            throw new InvalidOperationException($"Non-nullable record properties for Type '{type.Name}' not found");

        return properties;
    }
}