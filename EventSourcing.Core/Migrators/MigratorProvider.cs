using System.Reflection;

namespace EventSourcing.Core.Migrations;

public sealed class MigratorProvider
{
    private static readonly MigratorProvider instance = new MigratorProvider();

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static MigratorProvider() { }
    private MigratorProvider() { }
    public static MigratorProvider Instance => instance;
    
    
    
    private List<Type> AssemblyMigratorTypes => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(IRecordMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
        .ToList();
    
    private Dictionary<Type, IRecordMigrator?> _migrators;
    public Dictionary<Type, IRecordMigrator?> Migrators =>
        _migrators ?? throw new InvalidOperationException($"{nameof(MigratorProvider)} is not initialized. Run {nameof(Initialize)} first.");

    public bool Initialized;
    public void Initialize(List<Type>? migratorTypes = null)
    {
        // Create dictionary mapping from Record.Type to Migrator Type
        _migrators = (migratorTypes ?? AssemblyMigratorTypes)
            .Select(type => Activator.CreateInstance(type) as IRecordMigrator)
            .ToDictionary(migrator => migrator!.Source, migrator => migrator);

        Initialized = true;
    }
}