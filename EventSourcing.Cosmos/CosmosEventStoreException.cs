using System;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Cosmos
{
  public class CosmosEventStoreException : EventStoreException
  {
    public CosmosEventStoreException(string message, Exception innerException = null) : base(message, innerException)
    {
    }
  }
}