namespace EventSourcing.Core.Exceptions;

public class EventStoreException : Exception
{
  public EventStoreException() { }
  public EventStoreException(string message) : base(message) { }
  public EventStoreException(string message, Exception inner) : base(message, inner) { }
}