using System.Reflection;

namespace EventSourcing.Core.Types;

public sealed class RecordTypeProvider
{
    private static readonly RecordTypeProvider instance = new RecordTypeProvider();
    public static RecordTypeProvider Instance => instance;
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static RecordTypeProvider() { }
    private RecordTypeProvider() { }
    
    
    // Following is the non-static part of the class
    private readonly List<Type> _assemblyRecordTypes = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
        .ToList();
    
    private Dictionary<string, Type>? _recordTypes;
    private Dictionary<Type, string>? _recordTypesRev;
    private Dictionary<Type, PropertyInfo[]>? _nonNullableRecordProperties;
    public Dictionary<Type, PropertyInfo[]> NonNullableRecordProperties =>
        _nonNullableRecordProperties ?? throw new InvalidOperationException($"{nameof(RecordTypeProvider)} is not initialized. Run {nameof(Initialize)} first.");

    public bool Initialized;
    public void Initialize(List<Type>? recordTypes = null)
    {
        if (Initialized)
            return;
        
        // Create dictionaries mapping from Record.Type string to Record Type and it's reverse
        _recordTypes = (recordTypes ?? _assemblyRecordTypes).ToDictionary(type => type.GetCustomAttribute<RecordType>()?.Value ?? type.Name);
        _recordTypesRev = _recordTypes.ToDictionary(kv => kv.Value, kv => kv.Key);
        
        // For each Record Type, create set of non-nullable properties for validation
        _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
            .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());

        Initialized = true;
    }
    
    public Type GetRecordType(string typeString)
    {
        if (_recordTypes == null)
            return _assemblyRecordTypes.FirstOrDefault(t => t.GetCustomAttribute<RecordType>()?.Value == typeString)
                ?? _assemblyRecordTypes.FirstOrDefault(t => t.Name == typeString)
                ?? throw new InvalidOperationException($"Record with Type '{typeString}' not found");

        // Get actual Record Type from Dictionary
        if (!_recordTypes.TryGetValue(typeString, out var type))
      
            // Throw Exception when Record Type is not found in Assembly or RecordConverterOptions
            throw new InvalidOperationException($"Record with Type '{typeString}' not found");

        return type;
    }
    
    public string GetRecordTypeString(Type type)
    {
        if(_recordTypesRev == null)
            return type.GetCustomAttribute<RecordType>()?.Value ?? GetType().Name;
        
        // Get actual Record Type from Dictionary
        if (!_recordTypesRev.TryGetValue(type, out var typeString))
      
            // Throw Exception when Record Type is not found in Assembly or RecordConverterOptions
            throw new InvalidOperationException($"Record name for '{type.Name}' not found");

        return typeString;
    }
}