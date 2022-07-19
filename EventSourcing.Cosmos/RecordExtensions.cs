using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Cosmos;

public static class RecordExtensions
{
  public static string GetId(this Record record) => record switch
  {
    Projection p => $"{p.Kind}|{p.BaseType}|{p.AggregateId}",
    Snapshot s => $"{s.Kind}|{s.AggregateId}[{s.Index}]",
    Event e => $"{e.Kind}|{e.AggregateId}[{e.Index}]",
    _ => throw new ArgumentException()
  };
}