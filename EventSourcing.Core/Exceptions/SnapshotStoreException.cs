namespace EventSourcing.Core;

public class SnapshotStoreException : RecordStoreException
{
  public SnapshotStoreException(string message, Exception? inner = null) : base(message, inner) { }
}