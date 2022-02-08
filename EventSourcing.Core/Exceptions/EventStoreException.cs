namespace EventSourcing.Core;

public class EventStoreException : RecordStoreException
{
  public EventStoreException(string message, Exception? inner = null) : base(message, inner) { }
}