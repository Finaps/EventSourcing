using System;

namespace EventSourcing.Cosmos
{
  public class CosmosException : Exception
  {
    public CosmosException(string message) : base(message) { }
  }
}