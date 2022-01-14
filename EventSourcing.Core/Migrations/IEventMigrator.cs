namespace EventSourcing.Core.Migrations;


/// <summary>
/// Event Migrator: Converting an <see cref="Event"/> to it's successive version
/// </summary>
public interface IEventMigrator
{
     /// <summary>
     /// Source: Type of the source <see cref="Event"/> to migrate
     /// </summary>
     Type Source { get; }
     /// <summary>
     /// Target: Type of the target <see cref="Event"/> to migrate to
     /// </summary>
     Type Target { get; }
     /// <summary>
     /// Convert: Convert an <see cref="Event"/> to it's successive version
     /// </summary>
     Event Convert(Event e);
}
