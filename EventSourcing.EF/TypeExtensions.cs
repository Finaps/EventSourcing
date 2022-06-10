using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.EF;

public static class TypeExtensions
{
  public static string EventTable(this Type type) => $"{type.Name}{nameof(Event)}";
  public static string SnapshotTable(this Type type) => $"{type.Name}{nameof(Snapshot)}";
}
