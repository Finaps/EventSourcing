namespace EventSourcing.Core.Tests.Mocks;

[RecordName("CustomEventName")]
public record AttributeEvent(string SomeString) : Event;
