namespace EventSourcing.Core;

public class RecordType : Attribute
{
    public string Value { get; }

    public RecordType(string eventName)
    {
        Value = eventName;
    }
}