using System.Linq.Expressions;

namespace Chrysippus.Clauses;

internal class ParameterCollectorVisitor : ExpressionVisitor
{
    private readonly List<ParameterExpression> _collected;

    /// <summary>
    /// Parameters
    /// </summary>
    internal IEnumerable<ParameterExpression> Parameters => _collected;

    internal ParameterCollectorVisitor()
    {
        _collected = new List<ParameterExpression>();
    }

    /// <summary>
    /// Clear to reuse visitor
    /// </summary>
    internal void Reset() => _collected.Clear();

    protected override Expression VisitParameter(ParameterExpression node)
    {
        _collected.Add(node);
        return base.VisitParameter(node);
    }
}