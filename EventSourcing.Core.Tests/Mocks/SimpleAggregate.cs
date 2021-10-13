using System;

namespace EventSourcing.Core.Tests.Mocks
{
    public class SimpleAggregate : Aggregate
    {
        public int Counter { get; private set; }
        protected override void Apply<TEvent>(TEvent e)
        {
            Counter++;
        }
    }
}