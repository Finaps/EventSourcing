using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests
{
  public class InMemoryViewStoreTests : ViewStoreTests
  {
    protected override IViewStore GetViewStore() => new InMemoryViewStore();
  }
}