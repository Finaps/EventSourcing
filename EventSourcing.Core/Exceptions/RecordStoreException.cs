namespace EventSourcing.Core;

public class RecordStoreException : Exception
{
  public RecordStoreException(string message, Exception? inner = null) : base(message, inner) { }
}