namespace EventSourcing.Core.Migrations;


public abstract class EventMigratorBase<TSourceEvent, TTargetEvent> : IEventMigrator 
    where TSourceEvent : Event
    where TTargetEvent : Event
{
    public Type Source => typeof(TSourceEvent);
    public Type Target => typeof(TTargetEvent);
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