namespace Finaps.EventSourcing.Core;

/// <summary>
/// Represents a <see cref="Projection"/> of an <see cref="Aggregate{TAggregate}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Projections can be used to query the current state of multiple <see cref="Aggregate{TAggregate}"/>s.
/// </para>
/// <para>
/// To create <see cref="Projection"/>s, refer to <see cref="ProjectionFactory{TAggregate,TProjection}"/>.
/// </para>
/// <para>
/// To query <see cref="Projection"/>s, refer to <see cref="IRecordStore"/>.
/// </para>
/// </remarks>
/// <seealso cref="ProjectionFactory{TAggregate,TProjection}"/>
/// <seealso cref="IRecordStore"/>
public abstract record Projection : Record
{
  /// <summary>
  /// Base Type of Projection Hierarchy
  /// </summary>
  public string BaseType => Cache.GetProjectionBaseType(GetType()).Name;

  /// <summary>
  /// Factory type string
  /// </summary>
  public string? FactoryType { get; init; }

  /// <summary>
  /// The number of <see cref="Event"/>s applied to the source <see cref="Aggregate{TAggregate}"/>.
  /// </summary>
  public long Version { get; init; }
  
  /// <summary>
  /// Hash representing the code used to generate this <see cref="Projection"/>
  /// </summary>
  /// <remarks>See <see cref="ProjectionFactory{TAggregate,TProjection}"/>.<see cref="ProjectionFactory{TAggregate,TProjection}.ComputeHash"/> for more information</remarks>
  public string? Hash { get; init; }

  /// <summary>
  /// Compares the <see cref="Projection"/>.<see cref="Projection.Hash"/> (i.e. the state of the code at time of <see cref="Projection"/> creation)
  /// to the <see cref="Cache"/> (i.e. the current state of the code)
  /// to see whether this <see cref="Projection"/> is up to date.
  /// </summary>
  /// <remarks>To update projections, refer to the <see cref="ProjectionUpdateService"/></remarks>
  public bool IsUpToDate => FactoryType != null && Cache.GetProjectionFactoryHash(FactoryType) == Hash;

  /// <inheritdoc />
  public override string id => $"{Kind}|{BaseType}|{AggregateId}";
}