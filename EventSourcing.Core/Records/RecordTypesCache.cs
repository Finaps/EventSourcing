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
    private static readonly Dictionary<Type, string> RecordTypesRev;
    static RecordTypesCache()
    {
        // Create dictionaries mapping from Record.Type to Record Type string
        RecordTypesRev = AssemblyRecordTypes.ToDictionary(type => type, type => type.GetCustomAttribute<RecordType>()?.Value ?? type.Name);
    }
    
    // Non-static RecordTypes cache
    private readonly Dictionary<string, Type> _recordTypes;
    private readonly Dictionary<Type, string> _recordTypesRev;
    private readonly Dictionary<Type, PropertyInfo[]> _nonNullableRecordProperties;

    public RecordTypesCache(IReadOnlyCollection<Type>? recordTypes)
    {
        // Create dictionaries mapping from Record.Type string to Record Type and it's reverse
        _recordTypesRev = recordTypes == null ? 
            RecordTypesRev : recordTypes.ToDictionary(type => type, type => type.GetCustomAttribute<RecordType>()?.Value ?? type.Name);
        _recordTypes = _recordTypesRev.ToDictionary(kv => kv.Value, kv => kv.Key);
        // For each Record Type, create set of non-nullable properties for validation
        _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
            .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    }
    public Type GetRecordType(string typeString)
    {
        if (!_recordTypes.TryGetValue(typeString, out var type))
            throw new InvalidOperationException($"Record with Type '{typeString}' not found");

        return type;
    }
    public string GetRecordTypeString(Type type) => GetRecordTypeStringStatic(type, _recordTypesRev);
    public static string GetRecordTypeStringStatic(Type type, Dictionary<Type, string>? recordTypesRev = null)
    {
        if(!(recordTypesRev ?? RecordTypesRev).TryGetValue(type, out var typeString))
            throw new InvalidOperationException($"Record type for Type '{type.Name}' not found");

        return typeString;
    }
    public IEnumerable<PropertyInfo> GetNonNullableRecordProperties(Type type)
    {
        if(!_nonNullableRecordProperties.TryGetValue(type, out var properties))
            throw new InvalidOperationException($"Non-nullable record properties for Type '{type.Name}' not found");

        return properties;
    }
}