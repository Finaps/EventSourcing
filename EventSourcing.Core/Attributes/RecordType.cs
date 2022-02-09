namespace EventSourcing.Core;

public class RecordType : Attribute
{
    public string Value { get; }

    public RecordType(string recordType)
    {
        if(string.IsNullOrEmpty(recordType))
            throw new ArgumentNullException(nameof(recordType), "Record type cannot be an empty string");
        
        Value = recordType;
    }
}