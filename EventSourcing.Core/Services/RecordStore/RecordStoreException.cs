namespace Finaps.EventSourcing.Core;

/// <summary>
/// Record Store Exception
/// </summary>
public class RecordStoreException : Exception
{
  /// <inheritdoc />
  public RecordStoreException(string message, Exception? inner = null) : base(message, inner) { }
}