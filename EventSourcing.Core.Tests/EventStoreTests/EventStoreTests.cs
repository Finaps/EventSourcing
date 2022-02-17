using EventSourcing.Core.Services;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  protected abstract IEventStore EventStore { get; }
}