namespace EventSourcing.Core;

/// <summary>
/// Override for <see cref="Record"/>.<see cref="Record.Type"/> property.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RecordTypeAttribute : Attribute
{
  /// <summary>
  /// Overridden <see cref="Record"/>.<see cref="Record.Type"/>
  /// </summary>
  public string Type { get; }

  /// <summary>
  /// Create <see cref="RecordTypeAttribute"/>
  /// </summary>
  /// <param name="type">Custom <see cref="Record"/>.<see cref="Record.Type"/></param>
  /// <exception cref="ArgumentException">Thrown when <see cref="type"/> is null or whitespace</exception>
  public RecordTypeAttribute(string type)
  {
    if (string.IsNullOrWhiteSpace(type))
      throw new ArgumentException("Record type cannot be white space", nameof(type));

    Type = type;
  }
}