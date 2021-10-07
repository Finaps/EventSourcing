using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventSourcing.Core
{
  public static class Mapper
  {
    public static TDestination Map<TSource, TDestination>(TSource source, TDestination destination, IReadOnlySet<string> exclude = null)
    {
      foreach (var property in destination.GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => source.GetType().GetProperty(property.Name) != null && (exclude == null || !exclude.Contains(property.Name))))
        property.SetValue(destination, source.GetType().GetProperty(property.Name)?.GetValue(source));

      return destination;
    }
  }
}
