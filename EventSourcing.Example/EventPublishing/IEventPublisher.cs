using EventSourcing.Core;

namespace EventSourcing.Example.EventPublishing
{
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent @event) where TEvent : Event;
    }
}