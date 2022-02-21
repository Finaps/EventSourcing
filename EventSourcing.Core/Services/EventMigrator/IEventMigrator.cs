namespace EventSourcing.Core;

/// <summary>
/// Event Migrator: Converting an <see cref="Record"/> to it's successive version
/// </summary>
public interface IEventMigrator
{
  /// <summary>
  /// Source: Type of the source <see cref="Record"/> to migrate
  /// </summary>
  Type Source { get; }

  /// <summary>
  /// Target: Type of the target <see cref="Record"/> to migrate to
  /// </summary>
  Type Target { get; }

  /// <summary>
  /// Convert: Convert an <see cref="Record"/> to it's successive version
  /// </summary>
  Event Convert(Event record);
}