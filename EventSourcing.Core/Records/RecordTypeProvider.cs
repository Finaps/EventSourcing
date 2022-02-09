using System.Reflection;

namespace EventSourcing.Core.Types;

public sealed class RecordTypeProvider
{
    private static readonly RecordTypeProvider instance = new RecordTypeProvider();
    
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static RecordTypeProvider() { }
    private RecordTypeProvider() { }

    public static RecordTypeProvider Instance => instance;
    
    
    
    private List<Type> AssemblyRecordTypes => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
        .ToList();
    
    private Dictionary<string, Type> _recordTypes;
    private Dictionary<Type, string> _recordTypesRev;
    private Dictionary<Type, PropertyInfo[]> _nonNullableRecordProperties;
    public Dictionary<Type, PropertyInfo[]> NonNullableRecordProperties =>
        _nonNullableRecordProperties ?? throw new InvalidOperationException($"{nameof(RecordTypeProvider)} is not initialized. Run {nameof(Initialize)} first.");

    public void Initialize(List<Type>? recordTypes = null)
    {
        if (_recordTypes != null && _recordTypesRev != null && _nonNullableRecordProperties != null)
            throw new InvalidOperationException("Record types already initialized");
        
        // Create dictionaries mapping from Record.Type string to Record Type and it's reverse
        _recordTypes = (recordTypes ?? AssemblyRecordTypes).ToDictionary(type => type.GetCustomAttribute<RecordType>()?.Value ?? type.Name);
        _recordTypesRev = _recordTypes.ToDictionary(kv => kv.Value, kv => kv.Key);
        
        // For each Record Type, create set of non-nullable properties for validation
        _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
            .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    }
    
    public Type GetRecordType(string typeString)
    {
        if(_recordTypes == null)
            throw new InvalidOperationException($"{nameof(RecordTypeProvider)} is not initialized. Run {nameof(Initialize)} first.");
        
        // Get actual Record Type from Dictionary
        if (!_recordTypes.TryGetValue(typeString, out var type))
      
            // Throw Exception when Record Type is not found in Assembly or RecordConverterOptions
            throw new InvalidOperationException($"Record with Type '{typeString}' not found");

        return type;
    }
    
    public string GetRecordTypeString(Type type)
    {
        if(_recordTypesRev == null)
            throw new InvalidOperationException($"{nameof(RecordTypeProvider)} is not initialized. Run {nameof(Initialize)} first.");
        
        // Get actual Record Type from Dictionary
        if (!_recordTypesRev.TryGetValue(type, out var typeString))
      
            // Throw Exception when Record Type is not found in Assembly or RecordConverterOptions
            throw new InvalidOperationException($"Record name for '{type.Name}' not found");

        return typeString;
    }
}