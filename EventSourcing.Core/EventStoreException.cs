namespace EventSourcing.Core;

public class EventStoreException : Exception
{
  public EventStoreException(string message, Exception? inner = null) : base(message, inner) { }
}