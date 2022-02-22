using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
  [Fact]
  public async Task Can_Delete_View()
  {
    var view = new EmptyView { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate) };
    await RecordStore.AddViewAsync(view);

    Assert.NotNull(await RecordStore.GetViewByIdAsync<EmptyView>(view.AggregateId));

    await RecordStore.DeleteViewAsync<EmptyView>(view.AggregateId);

    await Assert.ThrowsAsync<RecordStoreException>(async () =>
      await RecordStore.GetViewByIdAsync<EmptyView>(view.AggregateId));
  }
}