using System.Linq.Expressions;
using Chrysippus.Clauses;

namespace Chrysippus.Kb.Evaluator.TruthTable;

/// <summary>
/// Evaluator truth-table based
/// </summary>
internal class TruthTableEvaluatorStrategy : IEvaluateStrategy
{
    private List<Expression> _clauses = new();
    private (IEnumerable<KbModelVersion> Model, IEnumerable<(int Id, IEnumerable<(string Symbol, bool Value)> Variables)> TruthTable)? _kbModel;

    public void ModelUpdate(IEnumerable<Expression> clauses)
    {
        _clauses = clauses.ToList();

        if (_clauses.Any())
        {
            _kbModel ??= EvalModel.Of(_clauses, EnumerateSymbols(_clauses));
        }
    }

    public bool? Entails(IEnumerable<Expression> alphaDisjoints)
    {
        // If Kb model is empty, no valid responses are possible
        if (!_kbModel.HasValue)
        {
            return null;
        }

        var alpha = alphaDisjoints.ToList().Aggregate(Expression.And);

        // Checking symbols consistency
        var symbols = EnumerateSymbols(_clauses);
        var alphaSymbols = EnumerateSymbols(alpha);
        if (!alphaSymbols.All(a => symbols.Contains(a)))
        {
            throw new UnknownSymbolException(
                $"Unknown symbol/s in Expression: {alpha}");
        }

        // Testing alpha where Kb model is true to answer (Kb |= α)
        var modelDetails = _kbModel!.Value.Model.GroupBy(m => m.Evaluation()).ToList();
        var whereModelIsTrue = modelDetails.FirstOrDefault(e => e.Key)?.ToList() ?? new List<KbModelVersion>();

        var alphaParams = EvalModel.EnumerateExpressionParameter(alpha).ToList();

        var tt = _kbModel.Value.TruthTable;

        return whereModelIsTrue.Any() && whereModelIsTrue.All(m =>
        {
            var variables = tt.
                Where(t =>
                    m.ClauseVariances.Any(mv =>
                        mv.TruthTableRowId == t.Id))
                .SelectMany(v =>
                    v.Variables.Where(pv =>
                        alphaParams.Any(p =>
                            p.Name == pv.Symbol)));

            return ExpressionEvaluatorFactory.Create(
                alpha,
                alphaParams.ToArray(),
                variables.Select(v => v.Value).ToArray()
            )();
        });
    }

    private IEnumerable<string> EnumerateSymbols(IEnumerable<Expression> e, HashSet<string>? symbols = null)
    {
        return EnumerateSymbols(e.Aggregate(Expression.And), symbols);
    }

    private IEnumerable<string> EnumerateSymbols(Expression e, HashSet<string>? symbols = null)
    {
        symbols ??= new HashSet<string>();

        if (e.IsBinary())
        {
            EnumerateSymbols(e.ToBinaryExpression().Left, symbols);
            EnumerateSymbols(e.ToBinaryExpression().Right, symbols);
        }
        else if(e.IsUnary())
        {
            EnumerateSymbols(e.ToUnaryExpression().Operand, symbols);
        }
        else if(e.IsParameter())
        {
            symbols.Add(e.ToParameterExpression().Name!);
        }

        return symbols;
    }
}