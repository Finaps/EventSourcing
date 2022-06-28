namespace Finaps.EventSourcing.Core.Tests.Mocks;

public record HierarchyEvent(string? A, string? B, string? C) : Event<HierarchyAggregate>;

public class HierarchyAggregate : Aggregate<HierarchyAggregate>
{
  public string? A { get; private set; }
  public string? B { get; private set; }
  public string? C { get; private set; }

  protected override void Apply(Event<HierarchyAggregate> e)
  {
    if (e is not HierarchyEvent(var a, var b, var c)) return;

    A = a;
    B = b;
    C = c;
  }
}

public record HierarchyProjection(string Total) : Projection;

public record HierarchyProjectionA(string Total, string A) : HierarchyProjection(Total);
public record HierarchyProjectionB(string Total, string B) : HierarchyProjection(Total);
public record HierarchyProjectionC(string Total, string C) : HierarchyProjection(Total);

public class HierarchyProjectionFactory : ProjectionFactory<HierarchyAggregate, HierarchyProjection>
{
  protected override HierarchyProjection CreateProjection(HierarchyAggregate aggregate)
  {
    // Create Projection A, B or C, depending on which field has the longest string
    var total = (aggregate.A ?? "") + (aggregate.B ?? "") + (aggregate.C ?? "");
    if (aggregate.A?.Length > aggregate.B?.Length && aggregate.A?.Length > aggregate.C?.Length)
      return new HierarchyProjectionA(total, aggregate.A ?? "");
    if (aggregate.B?.Length > aggregate.C?.Length)
      return new HierarchyProjectionB(total, aggregate.B ?? "");
    return new HierarchyProjectionC(total, aggregate.C ?? "");
  }
}