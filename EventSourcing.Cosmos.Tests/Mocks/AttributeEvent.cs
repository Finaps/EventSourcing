using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Cosmos.Tests.Mocks;

[RecordType("CustomEventName")]
public record AttributeEvent(string SomeString) : Event<EmptyAggregate>;
