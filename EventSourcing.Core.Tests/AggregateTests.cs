using System.Collections.Generic;
using EventSourcing.Core.Tests.Mocks;
using Xunit;

namespace EventSourcing.Core.Tests
{
    public class AggregateTests
    {
        
        [Fact]
        public void Can_Add_Event()
        {
            var aggregate = new SimpleAggregate();
            aggregate.Add(Event.Create<EmptyEvent>(aggregate));

            Assert.Single(aggregate.UncommittedEvents);
        }
        
        [Fact]
        public void Can_Add_Multiple_Events()
        {
            var aggregate = new SimpleAggregate();
            var events = new List<Event>
            {
                aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
                aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
                aggregate.Add(Event.Create<EmptyEvent>(aggregate))
            };

            Assert.Equal(events.Count, aggregate.UncommittedEvents.Length);
        }
        
        [Fact]
        public void Can_Clear_Uncommitted_Events()
        {
            var aggregate = new SimpleAggregate();
            aggregate.Add(Event.Create<EmptyEvent>(aggregate));
            aggregate.Add(Event.Create<EmptyEvent>(aggregate));
            aggregate.Add(Event.Create<EmptyEvent>(aggregate));

            aggregate.ClearUncommittedEvents();
            
            Assert.Empty(aggregate.UncommittedEvents);
        }
        
        [Fact]
        public void Can_Apply_Event()
        {
            var aggregate = new SimpleAggregate();
            aggregate.Add(Event.Create<EmptyEvent>(aggregate));
            
            Assert.Equal(1, aggregate.Counter);
        }
        
        [Fact]
        public void Can_Apply_Events()
        {
            var aggregate = new SimpleAggregate();
            var events = new List<Event>
            {
                aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
                aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
                aggregate.Add(Event.Create<EmptyEvent>(aggregate))
            };
            
            Assert.Equal(events.Count, aggregate.Counter);
        }
        
        [Fact]
        public void Can_Apply_From_History()
        {
            var aggregate = new SimpleAggregate();
            aggregate.Add(Event.Create<EmptyEvent>(aggregate), true);
            
            Assert.Equal(1, aggregate.Counter);
        }
        
        [Fact]
        public void Apply_From_History_Does_Not_Add_Uncommitted_Event()
        {
            var aggregate = new SimpleAggregate();
            aggregate.Add(Event.Create<EmptyEvent>(aggregate), true);
            
            Assert.Empty(aggregate.UncommittedEvents);
        }
    }
}