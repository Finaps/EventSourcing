using EventSourcing.Core.Tests;

namespace EventSourcing.EF.Tests;

public abstract partial class EntityFrameworkEventSourcingTests : EventSourcingTests
{
  public abstract RecordContext RecordContext { get; }
}