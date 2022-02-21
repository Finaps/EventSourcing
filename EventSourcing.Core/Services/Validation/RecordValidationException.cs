namespace EventSourcing.Core;

public class RecordValidationException : Exception
{
  public RecordValidationException(string message) : base(message) { }
}