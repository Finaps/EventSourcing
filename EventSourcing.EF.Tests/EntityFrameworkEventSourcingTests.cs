using Finaps.EventSourcing.Core.Tests;

namespace Finaps.EventSourcing.EF.Tests;

public abstract partial class EntityFrameworkEventSourcingTests : EventSourcingTests
{
  public abstract RecordContext GetRecordContext();
}