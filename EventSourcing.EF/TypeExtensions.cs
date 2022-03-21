using EventSourcing.Core;

namespace EventSourcing.EF;

internal static class TypeExtensions
{
  public static string EventTable(this Type type) => $"{type.Name}{nameof(Event)}s";
  public static string SnapshotTable(this Type type) => $"{type.Name}{nameof(Snapshot)}s";
}