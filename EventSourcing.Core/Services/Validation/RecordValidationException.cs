namespace Finaps.EventSourcing.Core;

/// <inheritdoc />
public class RecordValidationException : Exception
{
  /// <inheritdoc />
  public RecordValidationException(string message, Exception? inner = null) : base(message, null) { }
}