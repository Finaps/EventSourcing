namespace EventSourcing.Core;

public class RecordName : Attribute
{
    public string Value { get; }

    public RecordName(string eventName)
    {
        Value = eventName;
    }
}