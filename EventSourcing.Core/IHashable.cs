using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EventSourcing.Core;

public interface IHashable
{
  string ComputeHash();

  static string ComputeMethodHash(MethodInfo? method)
  {
    var data = method?.GetMethodBody()?.GetILAsByteArray();
    
    if (data == null) throw new NullReferenceException($"Cannot compute hash for {method}");

    return ByteArrayToString(MD5.HashData(data));
  }

  static string CombineHashes(params string[] hashes) =>
    ByteArrayToString(MD5.HashData(Encoding.ASCII.GetBytes(string.Concat(hashes))));

  private static string ByteArrayToString(IEnumerable<byte> source) =>
    string.Concat(source.Select(x => x.ToString("X2")));
}