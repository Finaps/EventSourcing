namespace EventSourcing.Core;

public static class Utils
{
  public static bool IsConsecutive(IList<ulong> numbers)
  {
    if (numbers.Count == 0) return true;

    var last = numbers[0];
    
    foreach (var number in numbers.Skip(1))
      if (number - last != 1) return false;
      else last = number;

    return true;
  }
}