using System.Collections;
using System.Reflection;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF;

internal static class TypeExtensions
{
  public static string EventTable(this Type type) => $"{type.Name}{nameof(Event)}s";
  public static string SnapshotTable(this Type type) => $"{type.Name}{nameof(Snapshot)}s";
  

  public static string? GetNotNullCheckConstraint(this Type type)
  {
    var eventProperties = typeof(Event).GetProperties().Select(x => x.Name);

    var properties = type
      .GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Where(property => property.PropertyType.HasStringOrValueType() && !eventProperties.Contains(property.Name))
      .Where(property => property.GetSetMethod() != null && property.GetGetMethod() != null && !property.IsNullable())
      .Select(x => x.Name)
      .ToList();

    if (!properties.Any()) return null;
    
    return $"NOT \"{nameof(Event.Type)}\" = '{type.Name}' OR " +
           $"({string.Join(" AND ", properties.Select(x => $"\"{x}\" IS NOT NULL"))})";
  }
  
  private static bool IsStringOrValueType(this Type type) => type.IsValueType || type == typeof(string);
  private static bool HasStringOrValueType(this Type type) =>
    (typeof(IEnumerable).IsAssignableFrom(type) ? type.GetGenericArguments().FirstOrDefault() ?? type : type).IsStringOrValueType();
}
