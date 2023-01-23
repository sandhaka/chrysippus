using System.Linq.Expressions;
using Chrysippus.Clauses;
using Chrysippus.Kb;
using Chrysippus.Kb.Evaluator;
using Xunit.Abstractions;

namespace Tests;

public class KbTests
{
    private readonly Kb _kb;
    private readonly ITestOutputHelper _testOutputHelper;

    public KbTests(ITestOutputHelper testOutputHelper)
    {
        _kb = new Kb();
        _testOutputHelper = testOutputHelper;
    }

    #region [ Kb Ausiliary & Internals ]

    [Fact]
    public void ShouldBeEquitable()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var d = Expression.Parameter(typeof(bool), "D");

        var a1 = Expression.Parameter(typeof(bool), "A");
        var b1 = Expression.Parameter(typeof(bool), "B");
        var c1 = Expression.Parameter(typeof(bool), "C");
        var d1 = Expression.Parameter(typeof(bool), "D");

        var first = Expression.And(Expression.Or(a, b), Expression.Or(c, d));
        var second = Expression.And(Expression.Or(a1, b1), Expression.Or(c1, d1));

        // Verify
        Assert.True(first.ToString() == second.ToString());
    }

    [Fact]
    public void ShouldMoveNotInwards()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        // ~(A | B) & A
        var statement = Expression.And(Expression.Not(Expression.Or(a, b)), a) as Expression;

        // Act
        var resolved = Cnf.MoveNotInwards(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);
        Assert.True(((BinaryExpression)resolved).Left.NodeType == ExpressionType.And);
        Assert.True(((BinaryExpression)((BinaryExpression)resolved).Left).Left.NodeType == ExpressionType.Not);

        _testOutputHelper.WriteLine($"Input -> ~(A | B) & A: {statement}");
        _testOutputHelper.WriteLine("Expected -> (~A & ~B) & A");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldMoveNotInwards1()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        // ~(~A & ~B)
        var statement = Expression.Not(Expression.And(Expression.Not(a), Expression.Not(b)));

        // Act
        var resolved = Cnf.MoveNotInwards(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.Or);
        Assert.True(resolved.ToBinaryExpression().Left.NodeType == ExpressionType.Parameter);
        Assert.True(resolved.ToBinaryExpression().Right.NodeType == ExpressionType.Parameter);

        _testOutputHelper.WriteLine($"Input -> ~(~A & ~B): {statement}");
        _testOutputHelper.WriteLine("Expected -> (A | B)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldConvertSimpleClauseToCnf()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        // ~(A & B)
        var statement = Expression.Not(Expression.And(a, b));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.False(resolved.NodeType == ExpressionType.Not);
        Assert.True(resolved.NodeType == ExpressionType.Or);

        _testOutputHelper.WriteLine($"Input -> ~(A & B): {statement}");
        _testOutputHelper.WriteLine("Expected -> (~A | ~B)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldConvertImplicationClauseToCnf()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");

        // A => (C & B) -> ~A | (C & B)
        var statement = a.Imply(Expression.And(c, b));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.False(resolved.NodeType == ExpressionType.Not);
        Assert.True(resolved.NodeType == ExpressionType.And);
        Assert.True(((BinaryExpression) resolved).Left.NodeType == ExpressionType.Or);
        Assert.True(((BinaryExpression) resolved).Right.NodeType == ExpressionType.Or);

        _testOutputHelper.WriteLine($"Input -> A => (C & B): {statement}");
        _testOutputHelper.WriteLine("Expected -> (~A | C) & (~A | B)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldConvertBiConditionalClauseToCnfMore()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");

        // (C & A) <=> (B & C)
        var statement = Expression.And(a, c).BiConditionals(Expression.And(b, c));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.False(resolved.NodeType == ExpressionType.Not);
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> (C & A) <=> (B & C): {statement}");
        _testOutputHelper.WriteLine("Expected -> (~A | ~C | B) & (A | ~B | ~C)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldDistributeAndOverOr()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");

        // A | (B & C)
        var statement = Expression.Or(a, Expression.And(b, c));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> A | (B & C): {statement}");
        _testOutputHelper.WriteLine("Expected -> (A | B) & (A | C)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldDistributeAndOverOr2()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var d = Expression.Parameter(typeof(bool), "D");

        // A & (B & C) | D
        var statement = Expression.Or(Expression.And(a, Expression.And(b, c)), d);

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> A & (B & C) | D: {statement}");
        _testOutputHelper.WriteLine("Expected -> (A | D) & (B | D) & (C | D)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldDistributeAndOverOr3()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var d = Expression.Parameter(typeof(bool), "D");
        var z = Expression.Parameter(typeof(bool), "Z");

        // Z & A & B & C | D
        var statement = Expression.Or(Expression.And(z, Expression.And(a, Expression.And(b, c))), d);

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> Z & A & (B & C) | D: {statement}");
        _testOutputHelper.WriteLine("Expected -> (Z | D) & (A | D) & (B | D) & (C | D)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldDistributeAndOverOrNested()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");

        // A | B | C | (A & B)
        var statement = Expression.Or(Expression.Or(c, Expression.Or(a, b)), Expression.And(a , b));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> A | B | C | (A & B): {statement}");
        _testOutputHelper.WriteLine("Expected -> (A | B | C | A) & (A | B | C | B)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldDistributeAndOverOrNested2()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var d = Expression.Parameter(typeof(bool), "D");

        // (((A & D) | B) | C) | (A & B)
        var statement = Expression.Or(Expression.Or(Expression.Or(Expression.And(a, d), b), c), Expression.And(a, b));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> (((A & D) | B) | C) | (A & B): {statement}");
        _testOutputHelper.WriteLine("Expected -> (A | B | C) & (D | B | C )");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    [Fact]
    public void ShouldDistributeAndOverOrAsDoubleDistributionCase()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var d = Expression.Parameter(typeof(bool), "D");
        // (A & B) | (C & D)
        var statement = Expression.Or(Expression.And(a, b), Expression.And(c, d));

        // Act
        var resolved = Cnf.ToCnfExpression(statement);

        // Verify
        Assert.True(resolved.NodeType == ExpressionType.And);

        _testOutputHelper.WriteLine($"Input -> (A & B) | (C & D): {statement}");
        _testOutputHelper.WriteLine("Expected -> (A | C) & (A | D) & (B | C) & (B | D)");
        _testOutputHelper.WriteLine($"Output -> {resolved}");
    }

    #endregion

    #region [ Kb ]

    [Fact]
    public void  ShouldThrowOnNotAllowableExpression()
    {
        var e = Expression.Modulo(Expression.Constant(2), Expression.Constant(3));
        var f =
            Expression.And(
                Expression.Parameter(typeof(int)),
                Expression.Add(
                    Expression.Parameter(typeof(int), "A"),
                    Expression.Parameter(typeof(int), "B")
                )
            );

        Assert.Throws<InvalidOperationException>(() => _kb.Tell(e));
        Assert.Throws<InvalidOperationException>(() => _kb.Tell(f));
    }

    [Fact]
    public void ShouldConsiderAnEmptyKb()
    {
        _kb.UseStrategy(PropositionalEvaluateStrategyType.TruthTable);

        Assert.Equal(InquireResponse.Unknown, _kb.Ask("A & B"));
    }

    [Fact]
    public void ShouldThrowOnStrategyUnselected()
    {
        Assert.Throws<InvalidOperationException>(() => _kb.Ask("A & B"));
    }

    [Fact]
    public void ShouldTellClauseToKb()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.TruthTable);
        _kb.TellMore("A & B", "(A | B) & Z", "(A & ( F & Y) | R)", "(H & J & I) | (K & U & L)", "(A & B) => C");
        _kb.Tell("Z <=> (X | Y)"); // Inefficient due to model enumeration

        // Act
        _kb.Retract("(H|U)");

        foreach (var e in _kb.Knowledge)
        {
            _testOutputHelper.WriteLine(e);
        }

        // Verify
        Assert.Contains(ClauseFactory.Create("A").ToString(), _kb.Knowledge);
        Assert.Contains(ClauseFactory.Create("A|B").ToString(), _kb.Knowledge);
        Assert.Contains(ClauseFactory.Create("I|U").ToString(), _kb.Knowledge);
        Assert.Contains(ClauseFactory.Create("L").ToString(), _kb.Knowledge);
        Assert.DoesNotContain(ClauseFactory.Create("(H|U)").ToString(), _kb.Knowledge);
    }

    #endregion

    #region [ Resolvers ]

    [Fact]
    public void ShouldAskByTruthTableResolver()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.TruthTable);
        _kb.Tell("A & B");
        _kb.Tell("(A | B) & Z");

        // Act
        var res = _kb.Ask("A");

        // Verify
        Assert.Equal(InquireResponse.True, res);
    }

    [Fact]
    public void ShouldAskByTruthTableResolver1()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.TruthTable);
        _kb.TellMore(
            "~P11",
            "~P12|B11",
            "~P21|B11",
            "P12|P21|~B11",
            "~P11|B21",
            "~P22|B21",
            "~P31|B21",
            "P11|P22|P31|~B21",
            "~B11",
            "B21"
        );

        // Act & Verify
        Assert.Equal(InquireResponse.True, _kb.Ask("~P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P31"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P22"));
        Assert.Equal(InquireResponse.True, _kb.Ask("B21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12 & ~P21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("P22 | P31"));
    }

    [Fact]
    public void ShouldAskByProofThroughContradiction()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.ProofByContradiction);
        _kb.Tell("A & B");
        _kb.Tell("(A | B) & Z");

        // Act
        var res = _kb.Ask("A");

        // Verify
        Assert.Equal(InquireResponse.True, res);
    }

    [Fact]
    public void ShouldAskByProofThroughContradiction2()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.ProofByContradiction);
        _kb.TellMore("P & Q", "P => R", "(Q & R) => S");

        // Act
        var res = _kb.Ask("S");
        var res1 = _kb.Ask("~S");

        // Verify
        Assert.Equal(InquireResponse.True, res);
        Assert.Equal(InquireResponse.False, res1);
    }

    [Fact]
    public void ShouldAskByProofThroughContradiction3()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.ProofByContradiction);
        _kb.TellMore(
            "~P11",
            "~P12|B11",
            "~P21|B11",
            "P12|P21|~B11",
            "~P11|B21",
            "~P22|B21",
            "~P31|B21",
            "P11|P22|P31|~B21",
            "~B11",
            "B21"
        );

        // Act & Verify
        Assert.Equal(InquireResponse.True, _kb.Ask("~P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P31"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P22"));
        Assert.Equal(InquireResponse.True, _kb.Ask("B21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12 & ~P21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("P22 | P31"));
    }

    [Fact]
    public void ShouldUsePropositionalKbCorrectly()
    {
        _kb.UseStrategy(PropositionalEvaluateStrategyType.ProofByContradiction);

        _kb.Tell("A & E");

        Assert.Equal(InquireResponse.True, _kb.Ask("A"));
        Assert.Equal(InquireResponse.True, _kb.Ask("E"));

        _kb.Tell("E => C"); // ~E | C

        Assert.Equal(InquireResponse.True, _kb.Ask("C"));

        _kb.Retract("E");

        Assert.Equal(InquireResponse.False, _kb.Ask("E"));
        Assert.Equal(InquireResponse.False, _kb.Ask("C"));
    }

    [Fact]
    public void ShouldAskChangingStrategyAtRuntime()
    {
        // Setup
        _kb.UseStrategy(PropositionalEvaluateStrategyType.ProofByContradiction);
        _kb.TellMore("~P11", "B11 <=> (P12 | P21)", "B21 <=> (P11 | P22 | P31)", "~B11", "B21");

        // Act & Verify
        Assert.Equal(InquireResponse.True, _kb.Ask("~P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P31"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P22"));
        Assert.Equal(InquireResponse.True, _kb.Ask("B21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12 & ~P21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("P22 | P31"));

        // Change strategy
        _kb.UseStrategy(PropositionalEvaluateStrategyType.TruthTable);

        // Act & Verify
        Assert.Equal(InquireResponse.True, _kb.Ask("~P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P11"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P31"));
        Assert.Equal(InquireResponse.False, _kb.Ask("P22"));
        Assert.Equal(InquireResponse.True, _kb.Ask("B21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12"));
        Assert.Equal(InquireResponse.True, _kb.Ask("~P12 & ~P21"));
        Assert.Equal(InquireResponse.True, _kb.Ask("P22 | P31"));
    }

    #endregion
}