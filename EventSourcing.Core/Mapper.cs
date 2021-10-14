using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventSourcing.Core
{
  /// <summary>
  /// Property Mapper
  /// </summary>
  public static class Mapper
  {
    /// <summary>
    /// Map properties from <c>source</c> to <c>destination</c>
    /// </summary>
    /// <param name="source">Source Object</param>
    /// <param name="destination">Destination Object</param>
    /// <param name="exclude">Set of properties to ignore</param>
    /// <typeparam name="TSource">Source Type</typeparam>
    /// <typeparam name="TDestination">Destination Type</typeparam>
    /// <returns>Destination Object</returns>
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
