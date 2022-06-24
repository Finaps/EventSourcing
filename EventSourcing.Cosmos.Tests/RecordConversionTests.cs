using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Finaps.EventSourcing.Core.Tests;

public class RecordConversionTests
{
  private record TestEvent : Event
  {
    public int A { get; init; }
    public int B { get; init; }
    public int C { get; init; }
    public int? D { get; set; }
  }

  [Fact]
  public Task Converter_Throws_On_Missing_And_Null_Properties_On_Read_And_Write()
  {
    var converter = new RecordConverter<Event>(new RecordConverterOptions {
      ThrowOnMissingNonNullableProperties = true
    });

    // Create Record with Missing and Null Values
    var faultyRecord = new TestEvent
    {
      PartitionId = Guid.NewGuid(),
      AggregateId = Guid.NewGuid(),
      AggregateType = "Test",

      A = 9,
      B = 10,
      C = 11
    };

    // Exception gets thrown when reading Faulty Record Json
    var exception = Assert.Throws<RecordValidationException>(() =>
    {
      var json = JsonSerializer.Serialize(faultyRecord)
        .Replace("\"B\":10,", "")  // Remove 'B' from json
        .Replace("11", "null");  // Set 'C' to null in json
      var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), true, default);
      return converter.Read(ref reader, typeof(Event), new JsonSerializerOptions());
    });

    Assert.DoesNotContain($"{nameof(TestEvent)}.PartitionId", exception.Message);
    Assert.DoesNotContain($"{nameof(TestEvent)}.AggregateId", exception.Message);
    Assert.DoesNotContain($"{nameof(TestEvent)}.AggregateType", exception.Message);
    Assert.DoesNotContain($"{nameof(TestEvent)}.Type", exception.Message);
    Assert.DoesNotContain($"{nameof(TestEvent)}.Index", exception.Message);
    Assert.DoesNotContain($"{nameof(TestEvent)}.Timestamp", exception.Message);

    // Validation should fail for:
    //  - TestRecord.B, since it is a non-nullable field, but null in reality
    //  - TestRecord.C, since it is a non-nullable field, but not present in JSON 
    Assert.DoesNotContain($"{nameof(TestEvent)}.A", exception.Message);
    Assert.Contains($"{nameof(TestEvent)}.B", exception.Message);
    Assert.Contains($"{nameof(TestEvent)}.C", exception.Message);
    Assert.DoesNotContain($"{nameof(TestEvent)}.D", exception.Message);

    return Task.CompletedTask;
  }
}