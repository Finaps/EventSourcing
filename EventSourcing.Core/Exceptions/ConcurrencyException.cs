namespace EventSourcing.Core.Exceptions;

public class ConcurrencyException : EventStoreException
{
    public ConcurrencyException() { }
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, Exception inner) : base(message, inner) { }

    public ConcurrencyException(Event e, Exception inner = null) : base(e == null
        ? "Conflict while adding Events"
        : $"Event '{e.Type}' with Version {e.AggregateVersion} already exists for Aggregate '{e.AggregateType}' with Id '{e.AggregateId}'", inner)
    {
    }
}