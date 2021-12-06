using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IViewStore
  {
    IQueryable<TView> Query<TAggregate, TView>() where TView : View<TAggregate>, new() where TAggregate : Aggregate, new();
    Task<TView> Get<TAggregate, TView>(Guid id, CancellationToken cancellationToken = default) where TView : View<TAggregate>, new() where TAggregate : Aggregate, new();
    Task UpsertAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : Aggregate;
  }
}
