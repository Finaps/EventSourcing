namespace EventSourcing.Core.Migrations;

public class RecordMigratorService
{
    private static List<Type> AssemblyMigratorTypes => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(IRecordMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
        .ToList();
    
    private readonly Dictionary<Type, IRecordMigrator?> _migrators;
    
    public RecordMigratorService(RecordConverterOptions? options = null)
    {
        // Create dictionary mapping from Record.Type to Migrator Type
        _migrators = (options?.MigratorTypes ?? AssemblyMigratorTypes)
            .Select(type => Activator.CreateInstance(type) as IRecordMigrator)
            .ToDictionary(migrator => migrator!.Source, migrator => migrator);
        
        ValidateMigrators();
    }
    
    public Record Migrate(Record record)
    {
        while (_migrators.TryGetValue(record.GetType(), out var migrator))
            record = migrator!.Convert(record);

        return record;
    }
    
    private void ValidateMigrators()
    {
        var migrations = _migrators.Values.ToDictionary(x => x!.Source, x => x!.Target);

        while (migrations.Count > 0)
        {
            var source = migrations.First().Key;
            var visited = new List<Type> { source };
      
            while (migrations.TryGetValue(source, out var target))
            {
                visited.Add(source);
                migrations.Remove(source);
        
                if (visited.Contains(target))
                    throw new ArgumentException("Record Migrator Collection contains cyclic reference(s)");
        
                source = target;
            }
        }
    }
}