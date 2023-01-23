using System.Linq.Expressions;
using Chrysippus.Clauses;
using Xunit.Abstractions;

namespace Tests;

public class ClauseTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ClauseTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private class Foo
    {
        public static Expression<Func<bool, bool>> CreatePropLogicExpr(Expression e, ParameterExpression in1) =>
            Expression.Lambda<Func<bool, bool>>(e, in1);
        public static Expression<Func<bool, bool, bool>> CreatePropLogicExpr(Expression e, ParameterExpression in1, ParameterExpression in2) =>
            Expression.Lambda<Func<bool, bool, bool>>(e, in1, in2);
        public static Expression<Func<bool, bool, bool, bool>> CreatePropLogicExpr(Expression e, ParameterExpression in1, ParameterExpression in2, ParameterExpression in3) =>
            Expression.Lambda<Func<bool, bool, bool, bool>>(e, in1, in2, in3);
    }

    [Fact]
    public void ShouldCreateAValidLogicClause()
    {
        // Setup
        var b11 = Expression.Parameter(typeof(bool), "B11");
        var p12 = Expression.Parameter(typeof(bool), "P12");
        var p21 = Expression.Parameter(typeof(bool), "P21");

        var statement = b11.BiConditionals(Expression.Or(p12, p21));

        var lambda = Foo.CreatePropLogicExpr(statement, b11, p12, p21);

        var compiled = lambda.Compile();

        _testOutputHelper.WriteLine($"Expression [B11 <=> (P12 | P21)] -> {statement}");

        // Act
        var result1 = compiled(false, false, false);

        // Verify
        Assert.True(result1);

        // Act (Make impossible)
        var result2 = compiled(false, true, false);

        Assert.False(result2);
    }

    [Fact]
    public void ShouldCreateAndLogicClause()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        var statement = Expression.And(a, b);
        var lambda = Foo.CreatePropLogicExpr(statement, a, b);

        var compiled = lambda.Compile();

        // Act
        var result1 = compiled(true, true);

        // Verify
        Assert.True(result1);

        // Act (Make impossible)
        var result2 = compiled(false, true);

        Assert.False(result2);
    }

    [Fact]
    public void ShouldCreateAndLogicClauseWithVisitor()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        var statement = Expression.And(a, b);

        var visitor = ClauseVisitorFactory.CreateParameterCollector();
        visitor.Visit(statement);
        var p = visitor.Parameters.ToList();

        var lambda = Foo.CreatePropLogicExpr(statement, p[1], p[0]); // Also inverted

        var compiled = lambda.Compile();

        // Act
        var result1 = compiled(true, true);

        // Verify
        Assert.True(result1);

        // Act (Make impossible)
        var result2 = compiled(false, true);

        Assert.False(result2);
    }

    [Fact]
    public void ShouldCreateLogicClauseWithSingleParam()
    {
        // Setup
        var a = Expression.Parameter(typeof(bool), "A");

        var statement = Expression.IsTrue(a);

        var lambda = Foo.CreatePropLogicExpr(statement, a);

        var compiled = lambda.Compile();

        // Act
        var result1 = compiled(true);

        // Verify
        Assert.True(result1);

        // Act (Make impossible)
        var result2 = compiled(false);

        Assert.False(result2);
    }

    [Fact]
    public void ShouldValidateExpressionSyntax()
    {
        Assert.Throws<InvalidOperationException>(() => ClauseFactory.Create("vcv|d@&fsg"));
    }

    [Fact]
    public void ShouldCreateClauseFromTyped()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        const string typed = "A | B";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.Or(a, b);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped2()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        const string typed = "A & B";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.And(a, b);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped3()
    {
        var a = Expression.Parameter(typeof(bool), "P11");
        var b = Expression.Parameter(typeof(bool), "P12");

        const string typed = "P11 & P12";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.And(a, b);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped4()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");

        const string typed = "C|(A&B)";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.Or(c, Expression.And(a, b));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped5()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var f = Expression.Parameter(typeof(bool), "F");

        const string typed = "(F | A) & (A & B) | ~A";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.Or(Expression.And(Expression.Or(f, a), Expression.And(a, b)), Expression.Not(a));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped6()
    {
        var c = Expression.Parameter(typeof(bool), "C");
        var h = Expression.Parameter(typeof(bool), "H");
        var f = Expression.Parameter(typeof(bool), "F");
        var b = Expression.Parameter(typeof(bool), "B");

        const string typed = "(F | (C & (B | H)))";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.Or(f, Expression.And(c, Expression.Or(b, h)));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped7()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        const string typed = "A => B";

        var expr = ClauseFactory.Create(typed);

        var e = a.Imply(b);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped9()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        const string typed = "A <=> B";

        var expr = ClauseFactory.Create(typed);

        var e = a.BiConditionals(b);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped10()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var r = Expression.Parameter(typeof(bool), "R");

        const string typed = "(A & R) <=> (B & B)";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.And(a,r).BiConditionals(Expression.And(b,b));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped11()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var r = Expression.Parameter(typeof(bool), "R");

        const string typed = "A & (B | C) & R";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.And(Expression.And(a, Expression.Or(b, c)), r);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped12()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var r = Expression.Parameter(typeof(bool), "R");

        const string typed = "A => ((B | C) & R)";

        var expr = ClauseFactory.Create(typed);

        var e = a.Imply(Expression.And(Expression.Or(b, c), r));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped13()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var r = Expression.Parameter(typeof(bool), "R");

        const string typed = "((B | C) & R) <=> A";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.And(Expression.Or(b, c), r).BiConditionals(a);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped14()
    {
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var r = Expression.Parameter(typeof(bool), "R");

        const string typed = "~((B | C) & ~R)";

        var expr = ClauseFactory.Create(typed);

        var e = Expression.Not(Expression.And(Expression.Or(b, c), Expression.Not(r)));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped16()
    {
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var a = Expression.Parameter(typeof(bool), "A");
        var d = Expression.Parameter(typeof(bool), "D");

        const string typed = "A & B & C | D";

        var expr = ClauseFactory.Create(typed);
        var e = Expression.Or(Expression.And(Expression.And(a, b), c), d);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped17()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var d = Expression.Parameter(typeof(bool), "D");
        var e1 = Expression.Parameter(typeof(bool), "E");
        var g = Expression.Parameter(typeof(bool), "G");
        var h = Expression.Parameter(typeof(bool), "H");

        const string typed = "((A & B) & C) | D & E & G & H";

        var expr = ClauseFactory.Create(typed);
        var e = Expression.And(Expression.And(Expression.And(Expression.Or(Expression.And(Expression.And(a, b), c), d), e1), g), h);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped18()
    {
        var b = Expression.Parameter(typeof(bool), "B");
        var c = Expression.Parameter(typeof(bool), "C");
        var a = Expression.Parameter(typeof(bool), "A");

        const string typed = "A | B | C | (A & B)";

        var expr = ClauseFactory.Create(typed);
        var e = Expression.Or(Expression.Or(Expression.Or(a, b), c), Expression.And(a, b));

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldCreateClauseFromTyped19()
    {
        var a = Expression.Parameter(typeof(bool), "A");
        var b = Expression.Parameter(typeof(bool), "B");

        const string typed = "~A|B";

        var expr = ClauseFactory.Create(typed);
        var e = Expression.Or(Expression.Not(a), b);

        _testOutputHelper.WriteLine($"Written: {expr}");
        _testOutputHelper.WriteLine($"To be: {e}");

        Assert.True(e.ToString() == expr.ToString());
    }

    [Fact]
    public void ShouldThrowOnInvalidInput()
    {
        const string typed = "((A & B) & C] | D & E & G & H";

        Assert.Throws<InvalidOperationException>(() => ClauseFactory.Create(typed));
    }

    [Fact]
    public void ShouldThrowOnInvalidInput2()
    {
        const string typed = "V&YU-&&&";

        Assert.Throws<InvalidOperationException>(() => ClauseFactory.Create(typed));
    }
}