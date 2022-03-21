using EventSourcing.Core;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Cosmos.Tests.Mocks;

[RecordType("CustomEventName")]
public record AttributeEvent : Event<EmptyAggregate>
{
  public string SomeString { get; set; }
}
