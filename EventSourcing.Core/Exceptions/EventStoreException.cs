using System.Net;

namespace EventSourcing.Core.Exceptions;

public class EventStoreException : Exception
{
  public EventStoreException() { }
  public EventStoreException(string message) : base(message) { }
  public EventStoreException(string message, Exception inner) : base(message, inner) { }
  public EventStoreException(HttpStatusCode status, Exception inner) : 
    base($"Encountered error while adding events: {(int)status} {status.ToString()}", inner) { }
}