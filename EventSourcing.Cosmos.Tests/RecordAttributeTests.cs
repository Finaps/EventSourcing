using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Core.Tests.Mocks;
using Finaps.EventSourcing.Cosmos.Tests.Mocks;
using Xunit;

namespace Finaps.EventSourcing.Cosmos.Tests;

public partial class CosmosEventSourcingTests
{
  [Fact]
  public async Task Can_Store_Attribute_Event_With_Correct_Type()
  {
    var e = new EmptyAggregate().Apply(new AttributeEvent("something"));
    var recordType = e.GetType().GetCustomAttribute<RecordTypeAttribute>()!.Type;

    await GetRecordStore().AddEventsAsync(new [] { e });
    var result = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.Type == recordType && x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync() as AttributeEvent;

    Assert.NotNull(result);
  }

  [Fact]
  public async Task Can_Deserialize_Attribute_Event_With_Custom_Type_Name()
  {
    var e = new EmptyAggregate().Apply(new AttributeEvent("something"));
    var recordType = e.GetType().GetCustomAttribute<RecordTypeAttribute>()!.Type;

    await GetRecordStore().AddEventsAsync(new [] { e });

    var result = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.Type == recordType && x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync() as AttributeEvent;

    Assert.NotNull(result);
    Assert.Equal(typeof(AttributeEvent).GetCustomAttribute<RecordTypeAttribute>()!.Type, result!.Type);
    Assert.Equal(e.SomeString, result.SomeString);
  }
}