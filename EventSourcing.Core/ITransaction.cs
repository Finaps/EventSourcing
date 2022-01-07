namespace EventSourcing.Core;

public interface ITransaction
{
  Task CommitAsync(CancellationToken cancellationToken);
}