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
  /// If true, RecordConverter will throw exception when not-nullable properties are not included or null in JSON
  /// </summary>
  public bool ThrowOnMissingNonNullableProperties { get; set; }
}