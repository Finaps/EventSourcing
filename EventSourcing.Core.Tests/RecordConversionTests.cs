using System.IO;
using System.Text;
using System.Text.Json;

namespace EventSourcing.Core.Tests;

public class RecordConversionTests
{
  private record TestRecord : IndexedRecord
  {
    public int A { get; init; }
    public string B { get; init; }
    public string C { get; init; }
    public double? D { get; set; }
  }

  [Fact]
  public Task Converter_Throws_On_Missing_And_Null_Properties_On_Read_And_Write()
  {
    var converter = new RecordConverter<IndexedRecord>(new RecordConverterOptions
    {
      RecordTypes = new List<Type> { typeof(TestRecord) }
    });

    // Create Record with Missing and Null Values
    var faultyRecord = new TestRecord
    {
      PartitionId = Guid.NewGuid(),
      AggregateId = Guid.NewGuid(),
      AggregateType = "Test",

      A = 9,
      B = null
    };
    
    // Exception gets thrown when writing Faulty Record Json
    var writeException = Assert.Throws<RecordValidationException>(() =>
    {
      using var writeStream = new MemoryStream();
      converter.Write(new Utf8JsonWriter(writeStream), faultyRecord, null);
    });

    // Exception gets thrown when reading Faulty Record Json
    var readException = Assert.Throws<RecordValidationException>(() =>
    {
      var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(faultyRecord)), true, default);
      return converter.Read(ref reader, typeof(IndexedRecord), default);
    });

    // Assert for both exceptions the right exception messages are shown
    foreach (var exception in new [] { writeException, readException })
    {
      Assert.DoesNotContain($"{nameof(TestRecord)}.PartitionId", exception.Message);
      Assert.DoesNotContain($"{nameof(TestRecord)}.AggregateId", exception.Message);
      Assert.DoesNotContain($"{nameof(TestRecord)}.AggregateType", exception.Message);
      Assert.DoesNotContain($"{nameof(TestRecord)}.Type", exception.Message);
      Assert.DoesNotContain($"{nameof(TestRecord)}.Index", exception.Message);
      Assert.DoesNotContain($"{nameof(TestRecord)}.Timestamp", exception.Message);

      // Validation should fail for:
      //  - TestRecord.B, since it is a non-nullable field, but null in reality
      //  - TestRecord.C, since it is a non-nullable field, but not present in JSON 
      Assert.DoesNotContain($"{nameof(TestRecord)}.A", exception.Message);
      Assert.Contains($"{nameof(TestRecord)}.B", exception.Message);
      Assert.Contains($"{nameof(TestRecord)}.C", exception.Message);
      Assert.DoesNotContain($"{nameof(TestRecord)}.D", exception.Message);
    }
    
    return Task.CompletedTask;
  }
}