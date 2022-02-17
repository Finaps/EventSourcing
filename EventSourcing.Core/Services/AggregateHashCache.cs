using System.Reflection;
using System.Security.Cryptography;
using EventSourcing.Core.Records;

namespace EventSourcing.Core.Services;

internal static class AggregateHashCache
{
  private static readonly Dictionary<Type, string> AggregateHashes = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(x => x.GetTypes())
    .Where(type => typeof(Aggregate).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
    .ToDictionary(type => type, type => ((Aggregate)Activator.CreateInstance(type)!).ComputeAggregateHash());

  public static string Get(Type type)
  {
    if (!AggregateHashes.TryGetValue(type, out var hash))
      throw new InvalidOperationException($"Error Getting Aggregate Hash for type '{type}'");
    return hash;
  }

  public static string GetMethodHash(Type type, string method)
  {
    var info = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance) ??
               throw new ArgumentException($"Couldn't find method {type}.{method} when computing hash");

    var body = info.GetMethodBody()?.GetILAsByteArray() ??
               throw new InvalidOperationException("Couldn't get Method IL ByteArray when computing hash");
    
    return string.Concat(MD5.HashData(body).Select(x => x.ToString("X2")));
  }
}