using System;
using System.Collections.Generic;

namespace EventSourcing.Core.Exceptions
{
  public class DuplicateKeyException : EventStoreException
  {
    public DuplicateKeyException() { }
    public DuplicateKeyException(string message) : base(message) { }
    public DuplicateKeyException(string message, Exception inner) : base(message, inner) { }
    public DuplicateKeyException(IEnumerable<Exception> innerExceptions) : base(innerExceptions) { }
    
    public static DuplicateKeyException CreateDuplicateVersionException(Event e) =>
      new($"Event '{e.Type}' with Version '{e.AggregateVersion}' already exists for Aggregate '{e.AggregateType}' with Id '{e.AggregateId}'");
  }
}