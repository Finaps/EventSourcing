namespace EventSourcing.Core;

/// <summary>
/// Migrate <see cref="Event"/>s to newer versions
/// </summary>
/// <remarks>
/// Use the <see cref="IEventMigrator"/> to migrate <see cref="Event"/>s to newer schema versions,
/// when incompatible schema updates are inevitable.
/// </remarks>
public interface IEventMigrator
{
  /// <summary>
  /// Type of the source <see cref="Event"/> to migrate
  /// </summary>
  Type Source { get; }

  /// <summary>
  /// Type of the target <see cref="Event"/> to migrate to
  /// </summary>
  Type Target { get; }

  /// <summary>
  /// Migrate an <see cref="Event"/> to a newer schema version
  /// </summary>
  /// <param name="e"><see cref="Event"/> to migrate</param>
  /// <returns>Migrated <see cref="Event"/></returns>
  Event Migrate(Event e);
}