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
    return Encoding.ASCII.GetString(MD5.HashData(data));
  }

  static string CombineHashes(params string[] hashes) =>
    Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(string.Concat(hashes))));
}