using System.Linq.Expressions;
using Chrysippus.Clauses;

namespace Chrysippus.Kb.Evaluator.TruthTable;

/// <summary>
/// Truth table model
/// </summary>
internal class KbModelVersion
{
    internal List<(int TruthTableRowId, Expression Clause, List<(ParameterExpression Expression, bool Value)> Parameters)> ClauseVariances { get; }
    internal Func<bool> Evaluation { get; }

    internal KbModelVersion(
        List<(int truthTableRowId, Expression Clause, List<(ParameterExpression Expression, bool Value)> Parameters)> model)
    {
        ClauseVariances = model;
        Evaluation = ClauseVariances.Select(clause =>
                ExpressionEvaluatorFactory.Create(
                    clause.Clause,
                    clause.Parameters.Select(v => v.Expression).ToArray(),
                    clause.Parameters.Select(v => v.Value).ToArray()
                )
            )
            .ToList()
            .Aggregate((_1,_2) => () => _1() && _2());
    }
}

internal class EvalModel
{
    private readonly List<KbModelVersion> _kbModel = new List<KbModelVersion>();
    private readonly IEnumerable<(int Id, IEnumerable<(string Symbol, bool Value)> Variables)> _truthTable;

    private EvalModel(IEnumerable<Expression> kbClauses, IEnumerable<string> symbols)
    {
        var kbc = kbClauses.ToList();
        var visitor = ClauseVisitorFactory.CreateParameterCollector();
        _truthTable = CreateTruthTable(symbols).ToList();

        foreach (var (id, variables) in _truthTable)
        {
            var expressions = new List<(int id, Expression Clause, List<(ParameterExpression Expression, bool Value)>)>();

            foreach (var clause in kbc)
            {
                visitor.Reset();
                visitor.Visit(clause);
                var parameters = visitor.Parameters.ToList();
                var pVariables = parameters.Select(p => (p, variables.Single(t => t.Symbol == p.Name).Value)).ToList();
                expressions.Add((id, clause, pVariables));
            }

            _kbModel.Add(new KbModelVersion(expressions));
        }
    }

    internal static (IEnumerable<KbModelVersion> Model, IEnumerable<(int Id, IEnumerable<(string Symbol, bool Value)> Variables)> TruthTable) Of(IEnumerable<Expression> kbClauses, IEnumerable<string> symbols)
    {
        var model = new EvalModel(kbClauses, symbols);
        return (model._kbModel, model._truthTable);
    }

    internal static IEnumerable<ParameterExpression> EnumerateExpressionParameter(Expression e)
    {
        var paramsCollector = ClauseVisitorFactory.CreateParameterCollector();
        paramsCollector.Visit(e);
        return paramsCollector.Parameters.ToList();
    }

    private static IEnumerable<(int Id, IEnumerable<(string Symbol, bool Value)> Variables)> CreateTruthTable(IEnumerable<string> symbols)
    {
        var id = 0;
        var sim = symbols.ToList();
        var size = sim.Count;
        var combo = new List<(int, IEnumerable<(string symbol, bool value)>)>();

        var c = Convert.ToInt32(Math.Pow(2, size));
        for (var i = 0; i < c; i++)
        {
            var bin = Convert.ToString(~i, 2)
                .ToCharArray()
                .Reverse()
                .Select(d => int.Parse(d.ToString()) == 1)
                .ToList();

            combo.Add((id++, sim.Select(s => (s, bin.ElementAt(sim.IndexOf(s))))));
        }

        return combo;
    }
}