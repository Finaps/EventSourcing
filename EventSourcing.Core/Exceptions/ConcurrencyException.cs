using System;
using System.Collections.Generic;

namespace EventSourcing.Core.Exceptions
{
    public class ConcurrencyException : EventStoreException
    {
        public ConcurrencyException() { }
        public ConcurrencyException(string message) : base(message) { }
        public ConcurrencyException(string message, Exception inner) : base(message, inner) { }
        public ConcurrencyException(IEnumerable<Exception> innerExceptions) : base(innerExceptions) { }
        
        public static ConcurrencyException CreateConcurrencyException(Event e) =>
            new($"Event '{e.Type}' with Version '{e.AggregateVersion}' already exists for Aggregate '{e.AggregateType}' with Id '{e.AggregateId}'");
    }
}