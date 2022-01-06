namespace EventSourcing.Core;

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
  public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> source)
  {
    return (IAsyncEnumerable<T>) source; 
  }

  /// <summary>
  /// Asynchronously Create a <see cref="List{T}"/> from source <see cref="IQueryable{T}"/>
  /// </summary>
  /// <remarks>
  /// This method assumes that the <see cref="IQueryable{T}"/> is a <see cref="IAsyncEnumerable{T}"/>
  /// </remarks>
  /// <param name="source">Source <see cref="IQueryable{T}"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="T">Subtype of source <c>IQueryable</c> and destination <c>IAsyncEnumerable</c></typeparam>
  /// <returns><see cref="Task{T}"/></returns>
  public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
  {
    var list = new List<T>();
    await foreach (var element in source.ToAsyncEnumerable().WithCancellation(cancellationToken))
      list.Add(element);
    return list;
  }
}