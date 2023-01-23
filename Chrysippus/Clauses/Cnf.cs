using System.Linq.Expressions;

namespace Chrysippus.Clauses;

/// <summary>
/// Conjunctions Normal Form conversion
/// </summary>
internal class Cnf
{
    /// <summary>
    /// Convert Expression Tree in a list of logical disjoints
    /// </summary>
    /// <param name="sentence">Expression</param>
    /// <returns>Cnf list</returns>
    internal static IEnumerable<Expression> ToCnf(Expression sentence)
    {
        return Conjuncts(DistributeAndOverOr(MoveNotInwards(sentence)));
    }

    /// <summary>
    /// Convert Expression Tree in a conjunction of disjoints
    /// </summary>
    /// <param name="sentence">Expression</param>
    /// <returns></returns>
    internal static Expression ToCnfExpression(Expression sentence)
    {
        return DistributeAndOverOr(MoveNotInwards(sentence));
    }

    internal static IEnumerable<Expression> Conjuncts(Expression sentence)
    {
        return DissociateConjuncts(sentence);
    }

    private static IEnumerable<Expression> DissociateConjuncts(Expression e, List<Expression>? conjuncts = null)
    {
        conjuncts ??= new List<Expression>();
        if (e.IsBinary() && e.ToBinaryExpression().IsAnd())
        {
            DissociateConjuncts(e.ToBinaryExpression().Left, conjuncts);
            DissociateConjuncts(e.ToBinaryExpression().Right, conjuncts);
        }
        else
        {
            conjuncts.Add(e);
        }

        return conjuncts;
    }

    private static Expression DistributeAndOverOr(Expression expr)
    {
        var q = new Queue<Expression>();

        Flatten(expr, q);

        var ret = SolveAndRaise(new Queue<Expression>(q.Reverse().ToList()));

        return ret;
    }

    private static void Flatten(Expression e, Queue<Expression> q)
    {
        if (e.IsBinary())
        {
            Flatten(e.ToBinaryExpression().Left, q);
            Flatten(e.ToBinaryExpression().Right, q);
        }

        q.Enqueue(e);
    }

    private static Expression Raise(Queue<Expression> q)
    {
        var t = q.Dequeue();

        if (t.IsBinary())
        {
            var right = Raise(q);
            var left = Raise(q);
            return t.ToBinaryExpression().IsAnd() ?
                Expression.And(left, right) :
                Expression.Or(left, right);
        }

        return t;
    }

    private static Expression SolveAndRaise(Queue<Expression> q)
    {
        var t = q.Dequeue();

        if (t.IsOperand())
        {
            return t;
        }

        var isOr = t.ToBinaryExpression().IsOr();
        var right = SolveAndRaise(q);
        var left = SolveAndRaise(q);

        if (isOr && right.IsBinary() && right.ToBinaryExpression().IsAnd())
        {
            return DistributeOrOverAllAnd(left, right);
        }

        if (isOr && left.IsBinary() && left.ToBinaryExpression().IsAnd())
        {
            return DistributeOrOverAllAnd(right, left);
        }

        var expr = t.ToBinaryExpression().IsAnd() ?
            Expression.And(left, right) :
            Expression.Or(left, right);

        return expr;
    }

    private static Expression DistributeOrOverAllAnd(Expression left, Expression right)
    {
        var leftDisjoints = DissociateConjuncts(left).ToList();
        var rightDisjoints = DissociateConjuncts(right).ToList();

        if (!leftDisjoints.Any())
        {
            return right;
        }
        if (!rightDisjoints.Any())
        {
            return left;
        }

        var list = new List<Expression>();

        foreach (var d in leftDisjoints)
        {
            foreach (var r in rightDisjoints)
            {
                list.Add(Expression.Or(d,r));
            }
        }

        return list.Aggregate(Expression.And);
    }

    internal static Expression MoveNotInwards(Expression expr)
    {
        switch (expr.NodeType)
        {
            case ExpressionType.Not:
            {
                var arg = ((UnaryExpression) expr).Operand;

                switch (arg.NodeType)
                {
                    case ExpressionType.And: // De Morgan
                    {
                        var leftOp = arg.ToBinaryExpression().Left;
                        var rightOp = arg.ToBinaryExpression().Right;

                        return Expression.Or(
                            MoveNotInwards(Expression.Not(leftOp)),
                            MoveNotInwards(Expression.Not(rightOp))
                        );
                    }
                    case ExpressionType.Or: // De Morgan
                    {
                        var leftOp = arg.ToBinaryExpression().Left;
                        var rightOp = arg.ToBinaryExpression().Right;

                        return Expression.And(
                            MoveNotInwards(Expression.Not(leftOp)),
                            MoveNotInwards(Expression.Not(rightOp))
                        );
                    }
                    case ExpressionType.Not: // ~~A
                    {
                        return MoveNotInwards(arg.ToUnaryExpression().Operand);
                    }
                    case ExpressionType.Parameter:
                    {
                        return expr;
                    }
                    default:
                        throw new InvalidOperationException($"Unexpected NodeType {arg.NodeType}");
                }
            }
            case ExpressionType.Or:
            {
                var binExpr = (BinaryExpression)expr;
                return Expression.Or(MoveNotInwards(binExpr.Left), MoveNotInwards(binExpr.Right));
            }
            case ExpressionType.And:
            {
                var binExpr = (BinaryExpression)expr;
                return Expression.And(MoveNotInwards(binExpr.Left), MoveNotInwards(binExpr.Right));
            }
            default:
                return expr;
        }
    }
}