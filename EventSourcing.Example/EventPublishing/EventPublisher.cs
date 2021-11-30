using System;
using System.Collections.Generic;
using EventSourcing.Core;

namespace EventSourcing.Example.EventPublishing
{
    public class EventPublisher
    {
        private readonly Dictionary<Type, IEventHandler> _commandHandlers = new();

        public void RegisterEventHandler<TEvent>(IEventHandler<TEvent> eventHandler) where TEvent : Event
        {
            _commandHandlers.Add(typeof(TEvent), eventHandler);
        }
        public void Publish<TEvent>(TEvent @event) where TEvent : Event
        {
            var handler = (IEventHandler<TEvent>) _commandHandlers[typeof(TEvent)];
            handler?.Handle(@event);
        }
    }
}