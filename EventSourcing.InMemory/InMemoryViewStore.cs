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

    public IQueryable<TView> Query<TView>()
      where TView : View, new() =>
      new InMemoryAsyncQueryable<TView>(_views[new TView().Type].Values
        .Select(AggregateToView<TView>)
        .AsQueryable());

    public Task<TView> Get<TView>(Guid id, CancellationToken cancellationToken = default)
      where TView : View, new() => 
      Task.FromResult(AggregateToView<TView>(_views[new TView().Type][id]));

    public Task UpsertAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new()
    {
      if (!_views.ContainsKey(aggregate.Type))
        _views[aggregate.Type] = new Dictionary<Guid, Aggregate>();

      _views[aggregate.Type][aggregate.Id] = aggregate;
      
      return Task.CompletedTask;
    }
    
    private static TView AggregateToView<TView>(Aggregate aggregate)
      where TView : View, new()
    {
      var view = new TView();
      
      foreach (var property in typeof(TView).GetProperties())
      {
        var source = aggregate.GetType().GetProperty(property.Name);
        if (source != null) property.SetValue(view, source.GetValue(aggregate));
      }

      return view;
    }
  }
}