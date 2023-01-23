namespace Chrysippus.Clauses;

internal static class ClauseVisitorFactory
{
    /// <summary>
    /// Expression Tree Visitor Factory
    /// </summary>
    /// <returns>Expression Tree visitor utility to collect parameters</returns>
    internal static ParameterCollectorVisitor CreateParameterCollector()
    {
        return new ParameterCollectorVisitor();
    }
}