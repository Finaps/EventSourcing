using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

/// <summary>
/// <see cref="DbContext"/> for Finaps.EventSourcing <see cref="Record"/> Types
/// </summary>
public class RecordContext : DbContext
{
  internal const string PreviousIndex = "PreviousIndex";
  internal const string ZeroIndex = "ZeroIndex";
  internal const int MaxTypeLength = 256;
  
  /// <inheritdoc />
  public RecordContext() {}

  /// <inheritdoc />
  public RecordContext(DbContextOptions options) : base(options) {}

  /// <summary>
  /// Find all <see cref="Record"/>s and add them to the <see cref="RecordContext"/>
  /// </summary>
  /// <param name="builder"><see cref="ModelBuilder"/></param>
  protected override void OnModelCreating(ModelBuilder builder)
  {
    // TODO: Make this easier to manually configure
    
    // Get all Records in Assembly
    var records = GetAssemblyTypes<Record>();

    // Get all Events, grouped by Aggregate Type
    var events = records
      .Where(type => typeof(Event).IsAssignableFrom(type))
      .GroupBy(GetAggregateType).Where(x => x.Key != null)
      .ToDictionary(grouping => grouping.Key!, grouping => grouping.ToList());
    
    // Get all Snapshots, grouped by Aggregate Type
    var snapshots = records
      .Where(type => typeof(Snapshot).IsAssignableFrom(type))
      .GroupBy(GetAggregateType).Where(x => x.Key != null)
      .ToDictionary(grouping => grouping.Key!, grouping => grouping.ToList());

    // Get all projections
    var projections = records
      .Where(type => typeof(Projection).IsAssignableFrom(type))
      .ToList();

    // For each Aggregate Type, create one table containing all Events and another containing all Snapshots
    // See https://docs.microsoft.com/en-us/ef/core/modeling/inheritance#table-per-hierarchy-and-discriminator-configuration
    foreach (var aggregateType in events.Keys)
    {
      var eventType = typeof(Event<>).MakeGenericType(aggregateType);
      builder.Entity(eventType, @event =>
      {
        // Create table for Event<TAggregate> -> "TAggregateEvents"
        @event.ToTable(aggregateType.EventTable());
        
        // Add Indices for AggregateType, Type and Timestamp
        @event.HasIndex(nameof(Event.AggregateType));
        @event.HasIndex(nameof(Event.Type));
        @event.HasIndex(nameof(Event.Timestamp));

        // Make Base Record Fields Required
        @event.Property(nameof(Event.Type)).IsRequired();
        @event.Property(nameof(Event.AggregateType)).IsRequired();
        @event.Property(nameof(Event.Timestamp)).IsRequired();
    
        // Constrain Type sizes
        @event.Property(nameof(Event.AggregateType)).HasMaxLength(MaxTypeLength);
        @event.Property(nameof(Event.Type)).HasMaxLength(MaxTypeLength);
        
        // Events are uniquely identified by their PartitionId, AggregateId, and Index
        @event.HasKey(nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index));
        
        // Event Indices should be non-negative, this is enforced through a check constraint
        var nonNegativeCheckConstraintName = $"CK_{aggregateType.EventTable()}_NonNegativeIndex";
        @event.HasCheckConstraint(nonNegativeCheckConstraintName, $"\"{nameof(Event.Index)}\" >= 0");
        @event.Property<long>(ZeroIndex).HasComputedColumnSql("cast(0 as bigint)", true);
        
        // Enforce Event Consecutiveness

        // 1. Add Computed Shadow Property which contains the Index of the previous Event
        // PreviousIndex is equal to Index - 1, except for Index == 0, then PreviousIndex = NULL
        @event.Property<long?>(PreviousIndex)
          .HasComputedColumnSql($"CASE WHEN \"{nameof(Event.Index)}\" = 0 THEN NULL ELSE \"{nameof(Event.Index)}\" - 1 END", true);
        
        // 2. Fix issue: SQL Server does not support filtered indices on computed columns
        // In step 3, we want to add a foreign key to the previous Event, using the PreviousIndex computed column
        // When adding a foreign key in EF Core, it automatically adds a unique index to this key
        // Since PreviousIndex is nullable, EF Core makes the unique index filtered on NOT NULL, which fails in SQL Server
        // The Index below is explicitly defined as not unique, which prevents EF Core from adding a filtered unique index 
        @event.HasIndex(nameof(Event.PartitionId), nameof(Event.AggregateId), PreviousIndex).IsUnique(false);
        
        // 3. Enforce Event Consecutiveness using a foreign key to the previous Event
        // This accomplishes three things:
        //  1. It disallows adding a loose Event
        //      e.g. adding Event with Index = 6 will fail when Event 5 does not exist.
        //  2. It disallows removing Events in the middle of the chain.
        //      i.e. if you want to remove Event 6, you are forced to remove Events after that as well
        //  3. No implicit deletions will occur.
        //      i.e. if you remove Event 0, a Cascade Delete would remove everything after, but that is now prevented.
        //      rather, the system expects the user to be explicit about removing Events
        var consecutiveIndexConstraintName = $"FK_{aggregateType.EventTable()}_ConsecutiveIndex";
        @event.HasOne(eventType).WithOne()
          .HasForeignKey(eventType, nameof(Event.PartitionId), nameof(Event.AggregateId), PreviousIndex)
          .HasConstraintName(consecutiveIndexConstraintName)
          .OnDelete(DeleteBehavior.Restrict);
      });
      
      var snapshotType = typeof(Snapshot<>).MakeGenericType(aggregateType);
      builder.Entity(snapshotType, snapshot =>
      {
        // Create table for Snapshot<TAggregate> -> "TAggregateSnapshots"
        snapshot.ToTable(aggregateType.SnapshotTable());
      
        // Add Indices for AggregateType, Type and Timestamp
        snapshot.HasIndex(nameof(Snapshot.AggregateType));
        snapshot.HasIndex(nameof(Snapshot.Type));
        snapshot.HasIndex(nameof(Snapshot.Timestamp));
        
        // Make Base Record Fields Required
        snapshot.Property(nameof(Snapshot.Type)).IsRequired();
        snapshot.Property(nameof(Snapshot.AggregateType)).IsRequired();
        snapshot.Property(nameof(Snapshot.Timestamp)).IsRequired();
    
        // Constrain Type sizes
        snapshot.Property(nameof(Snapshot.AggregateType)).HasMaxLength(MaxTypeLength);
        snapshot.Property(nameof(Snapshot.Type)).HasMaxLength(MaxTypeLength);

        // Snapshots are uniquely identified by their PartitionId, AggregateId, and Index
        snapshot.HasKey(nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index));
      
        // Snapshot Indices should be non-negative, this is enforced through a check constraint
        var nonNegativeCheckConstraintName = $"CK_{aggregateType.SnapshotTable()}_NonNegativeIndex";
        snapshot.HasCheckConstraint(nonNegativeCheckConstraintName, $"\"{nameof(Snapshot.Index)}\" >= 0");

        // Tie Snapshot to corresponding Event, such that when the Event is removed, the Snapshot is also removed
        snapshot.HasOne(typeof(Event<>).MakeGenericType(aggregateType)).WithOne()
          .HasForeignKey(snapshotType, nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index))
          .OnDelete(DeleteBehavior.Cascade);
      });
    }
    
    // Add all Events to their respective Event<TAggregate> hierarchies
    foreach (var type in events.Values.SelectMany(types => types))
      builder
        .Entity(type)
        .HasCheckConstraint($"CK_{type.Name}_NotNull", type.GetNotNullCheckConstraint())
        .HasDiscriminator<string>(nameof(Event.Type));
    
    // Add all Snapshots to their respective Snapshot<TAggregate> hierarchies
    foreach (var type in snapshots.Values.SelectMany(types => types))
      builder
        .Entity(type)
        .HasCheckConstraint($"CK_{type.Name}_NotNull", type.GetNotNullCheckConstraint())
        .HasDiscriminator<string>(nameof(Snapshot.Type));

    // Add all Projections as separate tables
    foreach (var type in projections)
    {
      EntityFrameworkRecordStore.ProjectionTypes.Add(type);
      builder.Entity(type, projection =>
      {
        // Add Indices for AggregateType, Type and Timestamp
        projection.HasIndex(nameof(Projection.AggregateType));
        projection.HasIndex(nameof(Projection.Type));
        projection.HasIndex(nameof(Projection.Timestamp));
        
        // Make Base Event Fields Required
        projection.Property(nameof(Projection.Type)).IsRequired();
        projection.Property(nameof(Projection.AggregateType)).IsRequired();
        projection.Property(nameof(Projection.Timestamp)).IsRequired();
    
        // Constrain Type sizes
        projection.Property(nameof(Projection.AggregateType)).HasMaxLength(MaxTypeLength);
        projection.Property(nameof(Projection.Type)).HasMaxLength(MaxTypeLength);
    
        // Constrain Aggregate Type size
        projection.Property(nameof(Projection.AggregateType)).HasMaxLength(MaxTypeLength);

        // Projections are uniquely identified by their PartitionId and AggregateId
        projection.HasKey(nameof(Projection.PartitionId), nameof(Projection.AggregateId));

        // To quickly find Projections with an outdated hash
        projection.HasIndex(nameof(Projection.Hash));
      
        // Hashes have a fixed length, better capitalize on that knowledge
        projection.Property(nameof(Projection.Hash)).HasMaxLength(IHashable.HashLength);
      });
    }
  }

  /// <summary>
  /// Get Aggregate Type for a given <see cref="Event"/> or <see cref="Snapshot"/>
  /// </summary>
  /// <param name="type"><see cref="Event"/> or <see cref="Snapshot"/></param>
  /// <returns></returns>
  private static Type? GetAggregateType(Type? type)
  {
    while (type != null)
    {
      var aggregateType = type.GetGenericArguments().FirstOrDefault(typeof(Aggregate).IsAssignableFrom);
      if (aggregateType != null) return aggregateType;
      type = type.BaseType;
    }

    return null;
  }

  /// <summary>
  /// See get all public, non-abstract types with a default constructor assignable from <see cref="T"/>
  /// </summary>
  /// <typeparam name="T"><see cref="Type"/></typeparam>
  /// <returns></returns>
  private static List<Type> GetAssemblyTypes<T>() => AppDomain.CurrentDomain
    .GetAssemblies().SelectMany(x => x.GetTypes())
    .Where(type => typeof(T).IsAssignableFrom(type) && type.IsPublic && type.IsClass && !type.IsAbstract &&
                   !type.IsGenericType &&
                   type.GetConstructor(Type.EmptyTypes) != null)
    .ToList();
}
