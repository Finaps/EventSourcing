using System.Linq.Expressions;
using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finaps.EventSourcing.EF;

public class EventCollectionBuilder<TAggregate> where TAggregate : Aggregate, new()
{
  private const string ZeroIndex = "ZeroIndex";
  private const string PreviousEvent = "_previousEvent";
  private const string PreviousIndex = "PreviousIndex";

  private readonly string TableName = $"{typeof(TAggregate).Name}{nameof(Event)}";

  private readonly ModelBuilder _modelBuilder;
  private readonly EntityTypeBuilder<Event<TAggregate>> _eventBuilder;

  public EventCollectionBuilder(ModelBuilder builder)
  {
    _modelBuilder = builder;

    // Add table for the Event<TAggregate> hierarchy
    // See https://docs.microsoft.com/en-us/ef/core/modeling/inheritance#table-per-hierarchy-and-discriminator-configuration
    _eventBuilder = builder.Entity<Event<TAggregate>>().ToTable(TableName);

    // Generic Record Configuration
    _eventBuilder.HasRecordIndices();

    // Events are uniquely identified by their PartitionId, AggregateId, and Index
    _eventBuilder.HasKey(x => new { x.PartitionId, x.AggregateId, x.Index });

    EnforceIndexNonNegativeness();
    EnforceIndexConsecutiveness();
  }

  public EventCollectionBuilder<TAggregate> Add<TEvent>() where TEvent : Event<TAggregate>
  {
    _modelBuilder.Entity<TEvent>().HasDiscriminator(x => x.Type);

    return this;
  }

  public ReferenceCollectionBuilder<Event<TForeignAggregate>, TEvent> AggregateReference<TEvent, TForeignAggregate>(
    Expression<Func<TEvent, Guid?>> navigation) where TEvent : Event<TAggregate> where TForeignAggregate : Aggregate, new()
  {
    var foreignAggregateId = navigation.GetSimpleMemberName();
    var foreignKeyName = $"FK_{typeof(TEvent).Name}_{foreignAggregateId}";
    
    // Add column of all zeros
    _eventBuilder.Property<long>(ZeroIndex).HasComputedColumnSql("cast(0 as bigint)", true);

    return _modelBuilder
      // Add one to many relation between Event<TForeignAggregate> and TEvent
      .Entity<TEvent>()
      .HasOne<Event<TForeignAggregate>>()
      .WithMany()

      // The foreign key { PartitionId, ForeignAggregateId, ZeroIndex } in TEvent
      // points to the Principal key { PartitionId, AggregateId, Index } in Event<TForeignAggregate>
      .HasForeignKey(nameof(Event.PartitionId), foreignAggregateId, ZeroIndex)
      .HasConstraintName(foreignKeyName)
      
      // Restrict deletion of Events by default
      .OnDelete(DeleteBehavior.Restrict)
      .IsRequired();
  }
  
  private void EnforceIndexNonNegativeness()
  {
    _eventBuilder.HasCheckConstraint($"CK_{TableName}_NonNegativeIndex", $"\"{nameof(Event.Index)}\" >= 0");
  }

  private void EnforceIndexConsecutiveness()
  {
    // Step 1.
    // Add Computed Shadow Property which contains the Index of the previous Event
    // PreviousIndex is equal to Index - 1, except for Index == 0, then PreviousIndex = NULL
    _eventBuilder.Property<long?>(PreviousIndex)
      .HasComputedColumnSql($"CASE WHEN \"{nameof(Event.Index)}\" = 0 THEN NULL ELSE \"{nameof(Event.Index)}\" - 1 END", true);
    
    // Step 2.
    // Fix issue: SQL Server does not support filtered indices on computed columns
    // In step 3, we want to add a foreign key to the previous Event, using the PreviousIndex computed column
    // When adding a foreign key in EF Core, it automatically adds a unique index to this key
    // Since PreviousIndex is nullable, EF Core makes the unique index filtered on NOT NULL, which fails in SQL Server
    // The Index below is explicitly defined as not unique, which prevents EF Core from adding a filtered unique index 
    _eventBuilder.HasIndex(nameof(Event.PartitionId), nameof(Event.AggregateId), PreviousIndex).IsUnique(false);

    // Step 3.
    // Enforce Event Consecutiveness using a foreign key to the previous Event
    // This accomplishes three things:
    //  1. It disallows adding a loose Event
    //      e.g. adding Event with Index = 6 will fail when Event 5 does not exist.
    //  2. It disallows removing Events in the middle of the chain.
    //      i.e. if you want to remove Event 6, you are forced to remove Events after that as well
    //  3. No implicit deletions will occur.
    //      i.e. if you remove Event 0, a Cascade Delete would remove everything after, but that is now prevented.
    //      rather, the system expects the user to be explicit about removing Events
    _eventBuilder
      .HasOne<Event<TAggregate>>(PreviousEvent)
      .WithOne()
      .HasForeignKey<Event<TAggregate>>(nameof(Event.PartitionId), nameof(Event.AggregateId), PreviousIndex)
      .HasConstraintName($"FK_{TableName}_ConsecutiveIndex")
      .OnDelete(DeleteBehavior.Restrict);
  }
}