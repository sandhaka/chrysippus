using Chrysippus.Kb.Evaluator.ProofByContradiction;
using Chrysippus.Kb.Evaluator.TruthTable;

namespace Chrysippus.Kb.Evaluator;

public static class PropositionalEvaluateStrategyType
{
    public static readonly string? TruthTable = typeof(TruthTableEvaluatorStrategy).AssemblyQualifiedName;
    public static readonly string? ProofByContradiction = typeof(ProofByContradictionStrategy).AssemblyQualifiedName;
}