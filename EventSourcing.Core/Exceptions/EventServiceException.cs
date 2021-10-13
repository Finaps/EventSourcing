using System;

namespace EventSourcing.Core.Exceptions
{
    public class EventServiceException : Exception
    {
        public EventServiceException(string message, Exception innerException = null) :
            base(innerException == null ? message : $"{message}. See the inner exception for details.", innerException) { }
    }
}