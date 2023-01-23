using System.Linq.Expressions;
using Chrysippus.Clauses;
using Chrysippus.Kb.Evaluator;

namespace Chrysippus.Kb;

/// <summary>
/// Propositional logic Knowledge base
/// </summary>
public class Kb: KnowledgeBase<Expression>
{
    private IEvaluateStrategy? _evaluateStrategy;

    /// <summary>
    /// Set the evaluation strategy used to prove a clause in the kb
    /// </summary>
    /// <param name="strategyType">A type of strategy <seealso cref="PropositionalEvaluateStrategyType"/>Strategy Type</param>
    public virtual void UseStrategy(string? strategyType)
    {
        if (string.IsNullOrEmpty(strategyType))
            throw new ArgumentNullException(nameof(strategyType));
        _evaluateStrategy = (IEvaluateStrategy?) Activator.CreateInstance(
            Type.GetType(strategyType) ?? throw new InvalidOperationException($"Invalid strategy: {strategyType}"));
        UpdateStrategyModel();
    }

    /// <summary>
    /// Add sentence in the knowledge base
    /// </summary>
    /// <param name="sentence">Clause Expression</param>
    public override void Tell(Expression sentence)
    {
        CheckExpressions(sentence);

        Clauses.AddRange(Cnf.ToCnf(sentence));

        UpdateStrategyModel();
    }

    /// <summary>
    /// Add sentences in the knowledge base
    /// </summary>
    /// <param name="sentences">Clause Expressions</param>
    public override void TellMore(params Expression[] sentences)
    {
        foreach (var sentence in sentences)
        {
            CheckExpressions(sentence);

            Clauses.AddRange(Cnf.ToCnf(sentence));
        }

        UpdateStrategyModel();
    }

    /// <summary>
    /// Add sentence in the knowledge base
    /// </summary>
    /// <param name="sentence">Clause string</param>
    public override void Tell(string sentence)
    {
        Tell(ClauseFactory.Create(sentence));
    }

    /// <summary>
    /// Add sentences in the knowledge base
    /// </summary>
    /// <param name="sentences">Clauses string</param>
    public void TellMore(IEnumerable<string> sentences)
    {
        TellMore(sentences.Select(ClauseFactory.Create).ToArray());
    }

    /// <summary>
    /// Add sentences in the knowledge base
    /// </summary>
    /// <param name="sentences">Clauses string</param>
    public override void TellMore(params string[] sentences)
    {
        TellMore(sentences.Select(ClauseFactory.Create).ToArray());
    }

    /// <summary>
    /// Remove sentence from the knowledge base
    /// </summary>
    /// <param name="sentence">Clause Expression</param>
    public override void Retract(Expression sentence)
    {
        CheckExpressions(sentence);

        var sentences = Cnf.ToCnf(sentence);

        foreach (var s in sentences)
        {
            // Literally equal -> A V B != B V A
            Clauses.RemoveAll(c => c.ToString() == s.ToString());
        }

        UpdateStrategyModel();
    }

    /// <summary>
    /// Remove sentence from the knowledge base
    /// </summary>
    /// <param name="sentence">Clause</param>
    public override void Retract(string sentence)
    {
        Retract(ClauseFactory.Create(sentence));
    }

    /// <summary>
    /// Remove sentences from the knowledge base
    /// </summary>
    /// <param name="sentences">Clauses</param>
    public void Retract(IEnumerable<string> sentences)
    {
        foreach (var sentence in sentences)
        {
            Retract(sentence);
        }
    }

    /// <summary>
    /// Verify if Kb |= α
    /// </summary>
    /// <param name="alpha">α</param>
    /// <returns>Kb entails alpha</returns>
    public InquireResponse Ask(string alpha)
    {
        return ToInquireResponse(GenAsk(ClauseFactory.Create(alpha)));
    }

    /// <summary>
    /// Verify if the Kb entails alpha
    /// </summary>
    /// <param name="alpha">α</param>
    /// <returns>Kb entails alpha</returns>
    public InquireResponse Ask(Expression alpha)
    {
        return ToInquireResponse(GenAsk(alpha));
    }

    protected virtual bool? Entails(IEnumerable<Expression> alpha)
    {
        if (_evaluateStrategy == null)
        {
            throw new InvalidOperationException("Strategy unselected");
        }

        return _evaluateStrategy.Entails(alpha);
    }

    private void UpdateStrategyModel()
    {
        _evaluateStrategy?.ModelUpdate(Clauses);
    }

    protected virtual bool? GenAsk(Expression alpha)
    {
        CheckExpressions(alpha);

        return Entails(Cnf.ToCnf(alpha));
    }

    private static void CheckExpressions(Expression e)
    {
        if (e.Type != typeof(bool))
        {
            throw new InvalidOperationException($"Expression type: {e.Type} not allowed");
        }

        if (e.IsBinary())
        {
            CheckExpressions(e.ToBinaryExpression().Left);
            CheckExpressions(e.ToBinaryExpression().Right);
        }
        else if(e.IsUnary())
        {
            CheckExpressions(e.ToUnaryExpression().Operand);
        }
        else if (!e.IsParameter())
        {
            throw new InvalidOperationException($"Expression Node Type: {e.NodeType} not allowed");
        }
    }

    private static InquireResponse ToInquireResponse(bool? answer)
    {
        InquireResponseResult result = answer;
        return result.Response;
    }
}