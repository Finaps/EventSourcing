using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IViewStore
  {
    IQueryable<TView> Query<TView>() where TView : View, new();
    Task<TView> Get<TView>(Guid id, CancellationToken cancellationToken = default) where TView : View, new();
    Task UpsertAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new();
  }
}
