namespace EventSourcing.Core;

public class RecordConverterOptions
{
  public List<Type>? RecordTypes { get; set; }
  public List<Type>? MigratorTypes { get; set; }
}