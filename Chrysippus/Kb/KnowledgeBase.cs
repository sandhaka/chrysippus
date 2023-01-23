using System.Linq.Expressions;

namespace Chrysippus.Kb;

public abstract class KnowledgeBase<TExpr> where TExpr : Expression
{
    protected List<TExpr> Clauses { get; }

    internal IEnumerable<string> Knowledge => Clauses.Select(c => c.ToString());

    protected KnowledgeBase(IEnumerable<TExpr>? sentences = null)
    {
        Clauses = sentences?.ToList() ?? new List<TExpr>();
    }

    public abstract void Retract(TExpr sentence);

    public abstract void Retract(string sentence);

    public abstract void Tell(TExpr sentence);

    public abstract void Tell(string sentence);

    public abstract void TellMore(params TExpr[] sentences);

    public abstract void TellMore(params string[] sentences);
}