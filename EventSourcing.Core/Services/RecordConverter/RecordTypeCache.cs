using System.Reflection;

namespace EventSourcing.Core;

public sealed class RecordTypeCache
{
    // Static RecordTypes cache
    private static readonly List<Type> AssemblyRecordTypes = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        .ToList();
    private static readonly Dictionary<Type, string> RecordTypeStrings =
        AssemblyRecordTypes.ToDictionary(type => type, type => type.GetCustomAttribute<RecordTypeAttribute>()?.Value ?? type.Name);

    // Non-static RecordTypes cache
    private readonly Dictionary<string, Type> _recordTypes;
    private readonly Dictionary<Type, string> _recordTypeStrings;
    private readonly Dictionary<Type, PropertyInfo[]> _nonNullableRecordProperties;

    public RecordTypeCache(IReadOnlyCollection<Type>? recordTypes)
    {
        // Create dictionaries mapping from Record.Type string to Record Type and it's reverse
        _recordTypeStrings = recordTypes == null ? 
            RecordTypeStrings : recordTypes.ToDictionary(type => type, type => type.GetCustomAttribute<RecordTypeAttribute>()?.Value ?? type.Name);
        _recordTypes = _recordTypeStrings.ToDictionary(kv => kv.Value, kv => kv.Key);
        // For each Record Type, create set of non-nullable properties for validation
        _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
            .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    }
    public Type GetRecordType(string typeString)
    {
        if (!_recordTypes.TryGetValue(typeString, out var type))
            throw new InvalidOperationException(
                $"Error getting record type string for {type}. {type} not provided in {nameof(RecordTypeCache)}.ctor");

        return type;
    }
    public string GetRecordTypeString(Type type)
    {
        if(!_recordTypeStrings.TryGetValue(type, out var typeString))
            throw new InvalidOperationException(
                $"Error getting record type string for {type}. {type} not provided in {nameof(RecordTypeCache)}.ctor");

        return typeString;
    }
    public static string GetAssemblyRecordTypeString(Type type)
    {
        if(!RecordTypeStrings.TryGetValue(type, out var typeString))
            throw new InvalidOperationException(
                $"Error getting record type string for {type}. {type} not found in Assembly. Ensure {type.Name} extends {typeof(Record)}");

        return typeString;
    }
    public IEnumerable<PropertyInfo> GetNonNullableRecordProperties(Type type)
    {
        if(!_nonNullableRecordProperties.TryGetValue(type, out var properties))
            throw new InvalidOperationException($"Error getting non-nullable properties for {type}. {type} not provided in {nameof(RecordTypeCache)}.ctor");

        return properties;
    }
}