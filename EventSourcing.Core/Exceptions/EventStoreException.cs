using System;
using System.Collections.Generic;

namespace EventSourcing.Core.Exceptions
{
  public class EventStoreException : AggregateException
  {
    public EventStoreException() { }
    public EventStoreException(string message) : base(message) { }
    public EventStoreException(string message, Exception inner) : base(message, inner) { }
    public EventStoreException(IEnumerable<Exception> innerExceptions) : base(innerExceptions) { }
    public EventStoreException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }
  }
}