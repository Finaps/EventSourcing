using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public interface IViewStore
  {
    IQueryable<TView> Query<TView>() where TView : IView, new();
    Task<TView> Get<TView>(Guid id, CancellationToken cancellationToken = default) where TView : IView, new();
    Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : IAggregate;
  }
}
