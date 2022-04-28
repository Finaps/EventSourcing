using System.Linq.Expressions;
using System.Reflection;
using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finaps.EventSourcing.EF;

public static class ModelBuilderExtensions
{
  public static ReferenceCollectionBuilder<Event<TAggregate>, TEvent> AggregateReference<TEvent, TAggregate>(
    this ModelBuilder builder, Expression<Func<TEvent, Guid?>> navigation) 
    where TEvent : Event where TAggregate : Aggregate, new()
  {
    var memberName = navigation.GetMemberAccess().GetSimpleMemberName();
    var constraintName = $"FK_{typeof(TEvent).Name}_{memberName}";

    return builder
      .Entity<TEvent>()
      .HasOne<Event<TAggregate>>()
      .WithMany()
      .HasForeignKey(nameof(Event.PartitionId), memberName, RecordContext.ZeroIndex)
      .HasConstraintName(constraintName)
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