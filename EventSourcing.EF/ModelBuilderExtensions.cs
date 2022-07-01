using System.Linq.Expressions;
using System.Reflection;
using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finaps.EventSourcing.EF;

/// <summary>
/// Finaps.EventSourcing specific extensions for EF Core ModelBuilder
/// </summary>
public static class ModelBuilderExtensions
{
  /// <summary>
  /// Create a relation between an Event and the first Event of another Aggregate
  /// </summary>
  /// <param name="builder"><see cref="ModelBuilder"/></param>
  /// <param name="navigation">Navigation to property that maps to related <c>AggregateId</c></param>
  /// <typeparam name="TEvent">Dependent Event Type</typeparam>
  /// <typeparam name="TAggregate">Principal Aggregate Type</typeparam>
  /// <returns><see cref="ReferenceCollectionBuilder"/> such that the reference can be further configured</returns>
  public static ReferenceCollectionBuilder<Event<TAggregate>, TEvent> AggregateReference<TEvent, TAggregate>(
    this ModelBuilder builder, Expression<Func<TEvent, Guid?>> navigation) 
    where TEvent : Event where TAggregate : Aggregate<TAggregate>, new()
  {
    var foreignAggregateId = navigation.GetMemberAccess().GetSimpleMemberName();
    var foreignKeyName = $"FK_{typeof(TEvent).Name}_{foreignAggregateId}";

    return builder
      .Entity<TEvent>()
      .HasOne<Event<TAggregate>>()
      .WithMany()
      .HasForeignKey(nameof(Event.PartitionId), foreignAggregateId, RecordContext.ZeroIndex)
      .HasConstraintName(foreignKeyName)
      .OnDelete(DeleteBehavior.Restrict)
      .IsRequired();
  }

  private static string GetSimpleMemberName(this MemberInfo member)
  {
    var name = member.Name;
    var index = name.LastIndexOf('.');
    return index >= 0 ? name[(index + 1)..] : name;
  }
}