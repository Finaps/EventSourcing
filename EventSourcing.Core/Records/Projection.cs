namespace EventSourcing.Core;

public record Projection : Record
{
  /// <summary>
  /// Factory type string
  /// </summary>
  public string? FactoryType { get; init; }
  
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; init; }
  
  /// <summary>
  /// Hash representing the code used to generate this <see cref="Projection"/>
  /// </summary>
  /// <remarks>See <see cref="ProjectionFactory{TAggregate,TProjection}"/>.<see cref="ProjectionFactory{TAggregate,TProjection}.ComputeHash"/> for more information</remarks>
  public string Hash { get; init; }

  /// <summary>
  /// Compares the <see cref="Projection"/>.<see cref="Projection.Hash"/> (i.e. the state of the code at time of <see cref="Projection"/> creation)
  /// to the <see cref="ProjectionCache"/>.<see cref="ProjectionCache.Hashes"/> (i.e. the current state of the code)
  /// to see whether this <see cref="Projection"/> is up to date.
  /// </summary>
  /// <remarks>To update projections, refer to the <see cref="ProjectionUpdateService"/></remarks>
  public bool IsUpToDate =>
    ProjectionCache.Hashes.TryGetValue(FactoryType ?? "", out var hash) && Hash == hash;

  public override string id => $"{Kind}|{Type}|{AggregateId}";
}