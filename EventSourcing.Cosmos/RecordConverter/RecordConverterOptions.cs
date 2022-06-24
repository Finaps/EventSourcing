namespace Finaps.EventSourcing.Core;

/// <summary>
/// <see cref="RecordConverter{TRecord}"/> Options
/// </summary>
public class RecordConverterOptions
{
  /// <summary>
  /// If true, RecordConverter will throw exception when not-nullable properties are not included or null in JSON
  /// </summary>
  public bool ThrowOnMissingNonNullableProperties { get; set; }
}