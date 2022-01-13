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
     /// Convert: Convert an <see cref="Event"/> to it's successive version
     /// </summary>
     Event Convert(Event e);
}

public abstract class EventMigrator<TSourceEvent, TTargetEvent> : IEventMigrator 
     where TSourceEvent : Event
     where TTargetEvent : Event
{
     public Type Source => typeof(TSourceEvent);
     public Event Convert(Event e) => 
          Convert(e as TSourceEvent) with
               {
                    Timestamp = e.Timestamp,
                    Type = typeof(TTargetEvent).Name,
                    AggregateId = e.AggregateId,
                    AggregateType = e.AggregateType,
                    AggregateVersion = e.AggregateVersion,
                    EventId = e.EventId,
                    PartitionId = e.PartitionId
               };

     public abstract TTargetEvent Convert(TSourceEvent e);
}
