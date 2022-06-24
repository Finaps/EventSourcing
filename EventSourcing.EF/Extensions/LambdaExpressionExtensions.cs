using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Finaps.EventSourcing.EF;

public static class LambdaExpressionExtensions
{
  public static string GetSimpleMemberName(this LambdaExpression expression)
  {
    var name = expression.GetMemberAccess().Name;
    var index = name.LastIndexOf('.');
    return index >= 0 ? name[(index + 1)..] : name;
  }
}