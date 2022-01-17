using System.Net;

namespace EventSourcing.Core;

public class EventStoreException : Exception
{
  public EventStoreException(string message) : base(message) { }
  public EventStoreException(string message, Exception inner) : base(message, inner) { }
  public EventStoreException(HttpStatusCode status, Exception inner) : 
    base($"Encountered error while adding events: {(int)status} {status.ToString()}", inner) { }
  public EventStoreException(Record r, Exception inner = null) : base(r == null
    ? "Conflict while adding Records"
    : $"{r.Type} with Version {r.Index} already exists for Aggregate '{r.AggregateType}' with Id '{r.AggregateId}'", inner)
  {
  }
}