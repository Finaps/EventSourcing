using System;
using System.Collections.Generic;

namespace EventSourcing.Core.Exceptions
{
  public class ViewStoreException : Exception
  {
    public ViewStoreException() { }
    public ViewStoreException(string message) : base(message) { }
    public ViewStoreException(string message, Exception inner) : base(message, inner) { }
  }
}