using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Cosmos;

/// <summary>
/// <see cref="Record"/> extensions
/// </summary>
public static class RecordExtensions
{
  /// <summary>
  /// Get Cosmos Record Id 
  /// </summary>
  /// <param name="record"><see cref="Record"/></param>
  /// <returns>id string</returns>
  /// <exception cref="ArgumentException"></exception>
  public static string GetId(this Record record) => record switch
  {
    Projection p => $"{p.Kind}|{p.BaseType}|{p.AggregateId}",
    Snapshot s => $"{s.Kind}|{s.AggregateId}[{s.Index}]",
    Event e => $"{e.Kind}|{e.AggregateId}[{e.Index}]",
    _ => throw new ArgumentException($"Invalid Record type. Record should be of type {nameof(Event)}, {nameof(Snapshot)} or {nameof(Projection)}. Found {record.GetType()}")
  };
}