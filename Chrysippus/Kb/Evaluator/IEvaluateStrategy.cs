using System.Linq.Expressions;

namespace Chrysippus.Kb.Evaluator;

internal interface IEvaluateStrategy
{
    void ModelUpdate(IEnumerable<Expression> clauses);
    bool? Entails(IEnumerable<Expression> alphaDisjoints);
}