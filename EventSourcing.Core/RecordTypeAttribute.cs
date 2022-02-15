namespace EventSourcing.Core;

[AttributeUsage(AttributeTargets.Class)]
public class RecordTypeAttribute : Attribute
{
  public string Value { get; }

  public RecordTypeAttribute(string recordType)
  {
    if (string.IsNullOrWhiteSpace(recordType))
      throw new ArgumentException("Record type cannot be white space", nameof(recordType));

    Value = recordType;
  }
}