namespace EventSourcing.Core;

/// <inheritdoc />
public class RecordValidationException : Exception
{
  /// <inheritdoc />
  public RecordValidationException(string message) : base(message) { }
}