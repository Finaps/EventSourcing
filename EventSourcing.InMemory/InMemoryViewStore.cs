using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;

namespace EventSourcing.InMemory
{
  public class InMemoryViewStore : IViewStore
  {
    private readonly Dictionary<string, Dictionary<Guid, Aggregate>> _views = new();

    public IQueryable<TView> Query<TAggregate, TView>()
      where TAggregate : Aggregate, new() where TView : View<TAggregate>, new() =>
      new InMemoryAsyncQueryable<TView>(_views[new TAggregate().Type].Values
        .Cast<TAggregate>()
        .Select(AggregateToView<TAggregate, TView>)
        .AsQueryable());

    public Task<TView> Get<TAggregate, TView>(Guid id, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new() where TView : View<TAggregate>, new() => 
      Task.FromResult(AggregateToView<TAggregate, TView>((TAggregate) _views[new TAggregate().Type][id]));

    public Task UpsertAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new()
    {
      if (!_views.ContainsKey(aggregate.Type))
        _views[aggregate.Type] = new Dictionary<Guid, Aggregate>();

      _views[aggregate.Type][aggregate.Id] = aggregate;
      
      return Task.CompletedTask;
    }
    
    private static TView AggregateToView<TAggregate, TView>(TAggregate aggregate)
      where TAggregate : Aggregate, new() where TView : View<TAggregate>, new()
    {
      var view = new TView();
      
      foreach (var property in typeof(TView).GetProperties())
      {
        var source = typeof(TAggregate).GetProperty(property.Name);
        if (source != null) property.SetValue(view, source.GetValue(aggregate));
      }

      return view;
    }
  }
}