using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Finaps.EventSourcing.Core;

public interface IHashable
{
  const int HashLength = 32;
  
  /// <summary>
  /// Compute Hash
  /// </summary>
  /// <returns>Hash string</returns>
  string ComputeHash();

  /// <summary>
  /// Compute hash for Method IL bytecode. Whenever the '<see cref="method"/>' source code changes, this hash changes.
  /// </summary>
  /// <remarks>Be aware this method does not recursively capture IL bytecode.
  /// i.e. if the contents of a method used inside of '<see cref="method"/>' changes,
  /// the bytecode and thus hash will stay the same.
  /// Use <see cref="CombineHashes"/> to combine the hashes of multiple methods if desired.
  /// </remarks>
  /// <param name="method">Method to compute hash for</param>
  /// <returns></returns>
  /// <exception cref="NullReferenceException"></exception>
  static string ComputeMethodHash(MethodInfo? method)
  {
    var data = method?.GetMethodBody()?.GetILAsByteArray();
    
    if (data == null) throw new NullReferenceException($"Cannot compute hash for {method}");

    return ByteArrayToString(MD5.HashData(data));
  }

  /// <summary>
  /// Combine two or more hashes into a single new hash
  /// </summary>
  /// <param name="hashes"></param>
  /// <returns></returns>
  static string CombineHashes(params string[] hashes) =>
    ByteArrayToString(MD5.HashData(Encoding.ASCII.GetBytes(string.Concat(hashes))));

  private static string ByteArrayToString(IEnumerable<byte> source) =>
    string.Concat(source.Select(x => x.ToString("X2")))[..HashLength];
}