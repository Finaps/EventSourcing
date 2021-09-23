using System.Linq;
using EventSourcing.Core;

namespace EventSourcing.Cosmos.Tests.Mocks
{
  public class MockEventFilter : IEventFilter<MockEvent>
  {
    public IQueryable<MockEvent> Filter(IQueryable<MockEvent> queryable) =>
      queryable.Where(x => x.MockBoolean);
  }
}
