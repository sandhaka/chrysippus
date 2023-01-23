using System.Linq.Expressions;

namespace Chrysippus.Kb.Evaluator;

/// <summary>
/// Create a lambda expression of expression tree
/// </summary>
internal class ExpressionEvaluatorFactory
{
    internal static Func<bool> Create(Expression e, ParameterExpression[] p, bool[] val)
    {
        var aExpr = e is ParameterExpression ? Expression.IsTrue(e) : e;

        return p.Length switch
        {
            1 => Create(aExpr, p[0], val[0]),
            2 => Create(aExpr, p[0], p[1], val[0], val[1]),
            3 => Create(aExpr, p[0], p[1], p[2], val[0], val[1], val[2]),
            4 => Create(aExpr, p[0], p[1], p[2], p[3], val[0], val[1], val[2], val[3]),
            5 => Create(aExpr, p[0], p[1], p[2], p[3], p[4], val[0], val[1], val[2], val[3], val[4]),
            6 => Create(aExpr, p[0], p[1], p[2], p[3], p[4], p[5], val[0], val[1], val[2], val[3], val[4], val[5]),
            _ => throw new NotImplementedException("Expression up to 6 parameters")
        };
    }

    private static Func<bool> Create(Expression e, ParameterExpression in1, bool val1) => () => Expression.Lambda<Func<bool, bool>>(e, in1).Compile()(val1);
    private static Func<bool> Create(Expression e, ParameterExpression in1, ParameterExpression in2, bool val1, bool val2) => () => Expression.Lambda<Func<bool, bool, bool>>(e, in1, in2).Compile()(val1, val2);
    private static Func<bool> Create(Expression e, ParameterExpression in1, ParameterExpression in2, ParameterExpression in3, bool val1, bool val2, bool val3) => () => Expression.Lambda<Func<bool, bool, bool, bool>>(e, in1, in2, in3).Compile()(val1, val2, val3);
    private static Func<bool> Create(Expression e, ParameterExpression in1, ParameterExpression in2, ParameterExpression in3, ParameterExpression in4, bool val1, bool val2, bool val3, bool val4) => () => Expression.Lambda<Func<bool, bool, bool, bool, bool>>(e, in1, in2, in3, in4).Compile()(val1, val2, val3, val4);
    private static Func<bool> Create(Expression e, ParameterExpression in1, ParameterExpression in2, ParameterExpression in3, ParameterExpression in4, ParameterExpression in5, bool val1, bool val2, bool val3, bool val4, bool val5) => () => Expression.Lambda<Func<bool, bool, bool, bool, bool, bool>>(e, in1, in2, in3, in4, in5).Compile()(val1, val2, val3, val4, val5);
    private static Func<bool> Create(Expression e, ParameterExpression in1, ParameterExpression in2, ParameterExpression in3, ParameterExpression in4, ParameterExpression in5, ParameterExpression in6,  bool val1, bool val2, bool val3, bool val4, bool val5, bool val6) => () => Expression.Lambda<Func<bool, bool, bool, bool, bool, bool, bool>>(e, in1, in2, in3, in4, in5, in6).Compile()(val1, val2, val3, val4, val5, val6);
}