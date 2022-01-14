namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  protected abstract IEventStore EventStore { get; }
  protected abstract IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() where TBaseEvent : Event, new();
}