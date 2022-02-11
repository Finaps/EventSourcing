namespace EventSourcing.Core;

public class RecordType : Attribute
{
    public string Value { get; }

    public RecordType(string recordType)
    {
        if(string.IsNullOrWhiteSpace(recordType))
            throw new ArgumentException("Record type cannot be white space", nameof(recordType));
        
        Value = recordType;
    }
}