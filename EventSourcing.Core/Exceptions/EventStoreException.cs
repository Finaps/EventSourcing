using System;

namespace EventSourcing.Core.Exceptions
{
  public class EventStoreException : Exception
  {
    public EventStoreException(string message, Exception innerException = null) :
      base(innerException == null ? message : $"{message}. See the inner exception for details.", innerException) { }
  }
}