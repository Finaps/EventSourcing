using System.Net;

namespace EventSourcing.Core;

public class EventStoreException : Exception
{
  public EventStoreException() { }
  public EventStoreException(string message) : base(message) { }
  public EventStoreException(string message, Exception inner) : base(message, inner) { }
  public EventStoreException(HttpStatusCode status, Exception inner) : 
    base($"Encountered error while adding events: {(int)status} {status.ToString()}", inner) { }
  public EventStoreException(Event e, Exception inner = null) : base(e == null
    ? "Conflict while adding Events"
    : $"Event '{e.Type}' with Version {e.AggregateVersion} already exists for Aggregate '{e.AggregateType}' with Id '{e.AggregateId}'", inner)
  {
  }
}