namespace EventSourcing.Core.Tests.Mocks;

[RecordType("CustomEventName")]
public record AttributeEvent(string SomeString) : Event;