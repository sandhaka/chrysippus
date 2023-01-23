using System.Linq.Expressions;
using Chrysippus.Clauses;

namespace Chrysippus.Kb.Evaluator.ProofByContradiction;

internal class ProofByContradictionStrategy : IEvaluateStrategy
{
    private List<Expression>? _clauses;

    public void ModelUpdate(IEnumerable<Expression> clauses)
    {
        _clauses = clauses.ToList();
    }

    public bool? Entails(IEnumerable<Expression> alphaDisjoints)
    {
        var workingSet = _clauses!.ToList();

        // If Kb model is empty, no valid responses are possible
        if (!_clauses?.Any() ?? false)
        {
            return null;
        }

        var alphaNegate = CnfNegate(alphaDisjoints.ToList().Aggregate(Expression.And));
        var alphaConjuncts = Cnf.ToCnf(alphaNegate);

        // Proof via refutation: Kb Λ ¬α
        workingSet.AddRange(alphaConjuncts);

        var resolvedSet = new List<Expression>();

        while (true)
        {
            var pairs = new List<(Expression i, Expression j)>();

            for (var i = 0; i < workingSet.Count - 1; i++)
            {
                for (var j = i + 1; j < workingSet.Count; j++)
                {
                    var pair = (workingSet[i], workingSet[j]);
                    pairs.Add(pair);
                }
            }

            foreach (var pair in pairs)
            {
                var resolvent = Resolution(pair).ToList();

                if (resolvent.Any(r => r.CnfEquality(Expression.Constant(false))))
                {
                    return true;
                }

                //
                var newR = resolvent.Where(rs => resolvedSet.All(rs.CnfInequality));

                resolvedSet.AddRange(newR);
            }

            if (resolvedSet.All(s => workingSet.Any(ws => ws.CnfEquality(s))))
            {
                return false;
            }

            workingSet.AddRange(resolvedSet.Where(rs => workingSet.All(rs.CnfInequality)));
        }
    }

    private IEnumerable<Expression> Resolution((Expression i, Expression j) pair)
    {
        var resolvent = new List<Expression>();

        var iDj = GetCnfDisjoints(pair.i).ToList();
        var jDj = GetCnfDisjoints(pair.j).ToList();

        foreach (var id in iDj)
        {
            foreach (var jd in jDj)
            {
                if (id.CnfEquality(CnfNegate(jd)) || CnfNegate(id).CnfEquality(jd))
                {
                    var ni = GetCnfDisjoints(pair.i).ToList();
                    var nj = GetCnfDisjoints(pair.j).ToList();

                    ni.RemoveAll(n => n.CnfEquality(id));
                    nj.RemoveAll(n => n.CnfEquality(jd));

                    var nClauses = ni.Concat(nj).DeleteCnfListDuplicates().ToList();

                    if (nClauses.Any())
                    {
                        resolvent.Add(nClauses.Aggregate(Expression.Or));
                    }
                    else
                    {
                        // This means: ¬X Λ X -> Contradiction
                        resolvent.Add(Expression.Constant(false));
                        return resolvent;
                    }
                }
            }
        }

        return resolvent;
    }

    private static IEnumerable<Expression> GetCnfDisjoints(Expression e, List<Expression>? disjoints = null)
    {
        disjoints ??= new List<Expression>();
        if (e.IsParameter() || e.IsUnary())
        {
            disjoints.Add(e);
        }
        else
        {
            GetCnfDisjoints(e.ToBinaryExpression().Left, disjoints);
            GetCnfDisjoints(e.ToBinaryExpression().Right, disjoints);
        }

        return disjoints;
    }

    private static Expression CnfNegate(Expression e)
    {
        if (e.NodeType == ExpressionType.Not)
        {
            return e.ToUnaryExpression().Operand;
        }

        return Expression.Not(e);
    }
}

internal static class CurrentStrategyExpressionExt
{
    internal static IEnumerable<Expression> DeleteCnfListDuplicates(this IEnumerable<Expression> sequence)
    {
        var seq = sequence.ToList();
        var cleaned = new List<Expression>();
        foreach (var expression in seq.Where(expression => cleaned.All(c => c.CnfInequality(expression))))
        {
            cleaned.Add(expression);
        }

        return cleaned;
    }

    internal static bool CnfEquality(this Expression l, Expression? r)
    {
        if (r is null)
        {
            return false;
        }

        if (ReferenceEquals(l, r))
        {
            return true;
        }

        // In case a disjoints expression is typed with different parameters ordering
        // (can happen during expression recombination)
        // For example, A V B == B V A is True
        var lDsj = CollectDisjoints(l).OrderByDescending(e => e.ToString()).ToList();
        var rDsj = CollectDisjoints(r).OrderByDescending(e => e.ToString()).ToList();

        return lDsj.Count == rDsj.Count && lDsj.All(ll => rDsj.Any(rr => ll.ToString() == rr.ToString()));
    }

    internal static bool CnfInequality(this Expression l, Expression r)
    {
        return !l.CnfEquality(r);
    }

    private static IEnumerable<Expression> CollectDisjoints(Expression e, List<Expression>? disjoints = null)
    {
        disjoints ??= new List<Expression>();
        if (e.IsParameter() || e.IsUnary() || e.IsConstant() || e.IsBinary() && e.ToBinaryExpression().IsAnd())
        {
            disjoints.Add(e);
        }
        else
        {
            CollectDisjoints(e.ToBinaryExpression().Left, disjoints);
            CollectDisjoints(e.ToBinaryExpression().Right, disjoints);
        }

        return disjoints;
    }
}