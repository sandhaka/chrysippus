using System.Linq.Expressions;

namespace Chrysippus.Clauses;

internal static class PropositionalExpressionExt
{
    public static Expression Imply(this Expression expr, Expression alpha)
    {
        var left = expr.IsUnary() ? expr : Expression.Not(expr); // Avoid ~~

        return Expression.Or(left, alpha);
    }

    public static Expression BiConditionals(this Expression expr, Expression alpha)
    {
        return Expression.And(
            expr.Imply(alpha),
            alpha.Imply(expr)
        );
    }

    public static bool IsAnd(this BinaryExpression expr)
    {
        return expr.NodeType == ExpressionType.And;
    }

    public static bool IsOr(this BinaryExpression expr)
    {
        return expr.NodeType == ExpressionType.Or;
    }

    public static BinaryExpression ToBinaryExpression(this Expression expr)
    {
        return (BinaryExpression) expr;
    }

    public static UnaryExpression ToUnaryExpression(this Expression expr)
    {
        return (UnaryExpression) expr;
    }

    public static ParameterExpression ToParameterExpression(this Expression expr)
    {
        return (ParameterExpression) expr;
    }

    public static bool IsOperand(this Expression expr)
    {
        return expr.IsParameter() || expr.IsUnary();
    }

    public static bool IsBinary(this Expression expr)
    {
        return expr is BinaryExpression;
    }

    public static bool IsUnary(this Expression expr)
    {
        return expr is UnaryExpression;
    }

    public static bool IsParameter(this Expression expr)
    {
        return expr.NodeType == ExpressionType.Parameter;
    }

    public static bool IsConstant(this Expression expr)
    {
        return expr is ConstantExpression;
    }
}