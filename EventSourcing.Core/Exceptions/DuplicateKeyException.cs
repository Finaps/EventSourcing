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
  }
}