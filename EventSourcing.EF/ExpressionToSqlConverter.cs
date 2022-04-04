using System.Linq.Expressions;
using System.Text;

namespace EventSourcing.EF;

internal sealed class SqlExpressionConverter : ExpressionVisitor
{
  private readonly StringBuilder _builder;

  public SqlExpressionConverter(Expression expression)
  {
    _builder = new StringBuilder();
    Visit(expression);
  }

  public override string ToString() => _builder.ToString();

  protected override Expression VisitUnary(UnaryExpression u)
  {
    switch (u.NodeType)
    {
      case ExpressionType.Not:
        _builder.Append(" NOT ");
        break;
      case ExpressionType.Convert:
        break;
      default:
        throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
    }
    
    Visit(u.Operand);

    return u;
  }

  protected override Expression VisitBinary(BinaryExpression b)
  {
    _builder.Append('(');
    
    Visit(b.Left);

    switch (b.NodeType)
    {
      case ExpressionType.And or ExpressionType.AndAlso:
        _builder.Append(" AND ");
        break;

      case ExpressionType.Or or ExpressionType.OrElse:
        _builder.Append(" OR ");
        break;

      case ExpressionType.Equal:
        _builder.Append(IsNullConstant(b.Right) ? " IS " : " = ");
        break;

      case ExpressionType.NotEqual:
        _builder.Append(IsNullConstant(b.Right) ? " IS NOT " : " <> ");
        break;

      case ExpressionType.LessThan:
        _builder.Append(" < ");
        break;

      case ExpressionType.LessThanOrEqual:
        _builder.Append(" <= ");
        break;

      case ExpressionType.GreaterThan:
        _builder.Append(" > ");
        break;

      case ExpressionType.GreaterThanOrEqual:
        _builder.Append(" >= ");
        break;

      default:
        throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
    }

    Visit(b.Right);
    _builder.Append(')');
    return b;
  }

  protected override Expression VisitConstant(ConstantExpression c)
  {
    if (c.Value == null)
    {
      _builder.Append("NULL");
      return c;
    }

    switch (Type.GetTypeCode(c.Value.GetType()))
    {
      case TypeCode.Boolean:
        _builder.Append((bool) c.Value ? 1 : 0);
        break;

      case TypeCode.String or TypeCode.DateTime:
        _builder.Append($"'{c.Value}'");
        break;
      
      case TypeCode.Int16:
        _builder.Append((short) c.Value);
        break;
      
      case TypeCode.Int32:
        _builder.Append((int) c.Value);
        break;
      
      case TypeCode.Int64:
        _builder.Append((long) c.Value);
        break;
      
      case TypeCode.Byte:
        _builder.Append((byte) c.Value);
        break;
        
      case TypeCode.UInt16:
        _builder.Append((ushort) c.Value);
        break;
      
      case TypeCode.UInt32:
        _builder.Append((uint) c.Value);
        break;
      
      case TypeCode.UInt64:
        _builder.Append((ulong) c.Value);
        break;

      case TypeCode.Object:
        throw new NotSupportedException($"The constant for '{c.Value}' is not supported");

      default:
        _builder.Append(c.Value);
        break;
    }

    return c;
  }

  protected override Expression VisitMember(MemberExpression m)
  {
    if (m.Expression is not { NodeType: ExpressionType.Parameter })
      throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");
    
    _builder.Append($"\"{m.Member.Name}\"");
    return m;
  }

  private static bool IsNullConstant(Expression exp)
  {
    return exp.NodeType == ExpressionType.Constant && ((ConstantExpression) exp).Value == null;
  }
}