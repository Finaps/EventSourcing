using System.Collections.Generic;

namespace EventSourcing.Core.Tests.Mocks
{
  public class VerboseAggregateView : View<VerboseAggregate>
  {
    public int NumberOfAppliedEvents { get; init; }
    public string HashDuringApply { get; init; }
  }
}