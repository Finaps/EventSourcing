namespace Finaps.EventSourcing.Core;

/// <summary>
/// Provides Extension Methods to work with IQueryable & IAsyncEnumerable types
/// </summary>
public static class QueryableExtensions
{
  /// <summary>
  /// Cast <see cref="IQueryable{T}"/> to <see cref="IAsyncEnumerable{T}"/>
  /// </summary>
  /// <remarks>
  /// This method assumes that the <see cref="IQueryable{T}"/> is a <see cref="IAsyncEnumerable{T}"/>
  /// </remarks>
  /// <param name="source">Source <see cref="IQueryable{T}"/></param>
  /// <returns><see cref="IAsyncEnumerable{T}"/></returns>
  public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IQueryable<T> source) => (IAsyncEnumerable<T>) source;
}