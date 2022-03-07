namespace EventSourcing.Core;

/// <summary>
/// ACID Aggregate Transaction.
/// </summary>
public interface IAggregateTransaction
{
  /// <summary>
  /// Add <see cref="Aggregate"/> to ACID <see cref="IAggregateTransaction"/>
  /// </summary>
  /// <remarks>
  /// When all <see cref="Aggregate"/>s have been added, call <see cref="CommitAsync"/> to commit them
  /// </remarks>
  /// <param name="aggregate"><see cref="Aggregate"/></param>
  /// <returns><see cref="IAggregateTransaction"/></returns>
  IAggregateTransaction Add(Aggregate aggregate);

  /// <summary>
  /// Commit <see cref="Aggregate"/>s to the <see cref="IRecordStore"/> in an ACID transaction.
  /// </summary>
  /// <exception cref="RecordStoreException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added <see cref="Aggregate"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task CommitAsync(CancellationToken cancellationToken = default);
}