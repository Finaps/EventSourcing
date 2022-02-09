namespace EventSourcing.Core.Migrations;

public class RecordMigratorService<TRecord> where TRecord : Record
{
    private MigratorProvider _migratorProvider = MigratorProvider.Instance;
    
    public RecordMigratorService(RecordConverterOptions? options = null)
    {
        if(!_migratorProvider.Initialized)
            _migratorProvider.Initialize(options?.MigratorTypes);
        
        ValidateMigrators();
    }
    
    public TRecord Migrate(TRecord record)
    {
        while (_migratorProvider.Migrators.TryGetValue(record.GetType(), out var migrator))
            record = (TRecord) migrator!.Convert(record);

        return record;
    }
    
    private void ValidateMigrators()
    {
        var migrations = _migratorProvider.Migrators.Values.ToDictionary(x => x!.Source, x => x!.Target);

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