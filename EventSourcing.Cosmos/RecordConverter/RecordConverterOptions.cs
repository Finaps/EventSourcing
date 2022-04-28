namespace Finaps.EventSourcing.Core;

/// <summary>
/// <see cref="RecordConverter{TRecord}"/> Options
/// </summary>
public class RecordConverterOptions
{
  /// <summary>
  /// <see cref="Record"/> types to use for deserialization.
  /// When not specified, <see cref="RecordConverter{TRecord}"/> will use all <see cref="Record"/> types in assembly.
  /// </summary>
  public List<Type>? RecordTypes { get; set; }
  
  /// <summary>
  /// <see cref="IEventMigrator"/> types to use for migration.
  /// When not specified, <see cref="RecordConverter{TRecord}"/> will use all <see cref="IEventMigrator"/> types in assembly.
  /// </summary>
  public List<Type>? MigratorTypes { get; set; }

  public bool ThrowOnMissingNonNullableProperties { get; set; }
}