using System;

namespace EventSourcing.Core.Exceptions
{
  public class JsonTypedConverterException : Exception
  {
    public JsonTypedConverterException() { }
    public JsonTypedConverterException(string message) : base(message) { }
    public JsonTypedConverterException(string message, Exception inner) : base(message, inner) { }
  }
}