using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Chrysippus.Clauses;

/// <summary>
/// Utility to convert string logical clause to Binary Expression Tree
/// </summary>
public static class ClauseFactory
{
    private static readonly string[] LegalTokens = { " ", "(", ")" };

    private static readonly string[] Operators = {"|","&","~","=>","<=>"};

    private static readonly string[] OperatorsRadix = {"|","&","~","=","<"};

    private static string Sym => LegalTokens.Concat(Operators).AsString();

    /// <summary>
    /// Create Expression Tree from string clause
    /// </summary>
    /// <param name="s">Clause typed</param>
    /// <returns>Converted Expression Tree</returns>
    /// <exception cref="InvalidOperationException">On illegal semantic or syntax</exception>
    internal static Expression Create(string s)
    {
        if (!Regex.Match(s.Where(_ => !Sym.Contains(_))
                    .Select(_ => _.ToString())
                    .Aggregate((a, b) => $"{a}{b}"),
                @"^[a-zA-Z0-9]+$",
                RegexOptions.IgnoreCase).Success
           )
        {
            throw new InvalidOperationException($"Invalid input: {s}");
        }

        try
        {
            var input = new Queue<char>(
                SetOperatorsPrecedenceWhenUndeclared(
                    s.Replace(" ", string.Empty)
                        .ToUpper()
                        .ToCharArray()
                        .ToList()
                )
            );

            return BootstrapExpr(input);

        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Error on parsing input: {s}", e);
        }
    }

    private static Expression BootstrapExpr(
        Queue<char> q,
        Stack<Expression>? eq = null,
        List<char>? buffer = null,
        List<string>? tokens = null,
        bool eJs = false)
    {
        tokens ??= new List<string>();
        buffer ??= new List<char>();
        eq ??= new Stack<Expression>();

        while (q.Any())
        {
            var c = q.Dequeue();

            switch (c)
            {
                case '=': // =>
                {
                    var token = $"{c}{q.Dequeue()}".Trim();

                    if (!token.Equals("=>"))
                    {
                        throw new InvalidOperationException(
                            $"Illegal token: '{token}' at {q.AsString()}{buffer.AsString()}");
                    }

                    if (buffer.Any())
                    {
                        tokens.Add(buffer.AsString());
                    }

                    buffer.Clear();

                    var t = tokens.Last();
                    Expression leftOperand = t == ")" ? null : Expression.Parameter(typeof(bool), tokens.Last());

                    tokens.Add(token);
                    var rightOperand = BootstrapExpr(q, eq, buffer, tokens, !Fop(tokens));

                    if (t == ")" && eq.Any())
                    {
                        leftOperand = eq.Pop();
                    }

                    if (!eq.Any() && q.Any() && q.First() != ')' && !eJs)
                    {
                        eq.Push(leftOperand.Imply(rightOperand));
                    }
                    else
                    {
                        return leftOperand.Imply(rightOperand);
                    }

                    break;
                }
                case '<': // <=>
                {
                    var token = $"{c}{q.Dequeue()}{q.Dequeue()}".Trim();

                    if (!token.Equals("<=>"))
                    {
                        throw new InvalidOperationException(
                            $"Illegal token: '{token}' at {q.AsString()}{buffer.AsString()}");
                    }

                    if (buffer.Any())
                    {
                        tokens.Add(buffer.AsString());
                    }

                    buffer.Clear();

                    var t = tokens.Last();

                    Expression leftOperand =
                        t == ")" ? null : Expression.Parameter(typeof(bool), tokens.Last());

                    tokens.Add(token);
                    var rightOperand = BootstrapExpr(q, eq, buffer, tokens, !Fop(tokens));

                    if (t == ")" && eq.Any())
                    {
                        leftOperand = eq.Pop();
                    }

                    if (!eq.Any() && q.Any() && q.First() != ')' && !eJs)
                    {
                        eq.Push(leftOperand.BiConditionals(rightOperand));
                    }
                    else
                    {
                        return leftOperand.BiConditionals(rightOperand);
                    }

                    break;
                }
                case '|':
                {
                    if (buffer.Any())
                    {
                        tokens.Add(buffer.AsString());
                    }

                    buffer.Clear();

                    var t = tokens.Last();

                    Expression leftOperand =
                        t == ")" ? null : Expression.Parameter(typeof(bool), tokens.Last());

                    tokens.Add(c.ToString());
                    var rightOperand = BootstrapExpr(q, eq, buffer, tokens, !Fop(tokens));

                    if (t == ")" && eq.Any())
                    {
                        leftOperand = eq.Pop();
                    }

                    if (!eq.Any() && q.Any() && q.First() != ')' && !eJs)
                    {
                        eq.Push(Expression.Or(leftOperand!, rightOperand));
                    }
                    else
                    {
                        return Expression.Or(leftOperand!, rightOperand);
                    }

                    break;
                }
                case '&':
                {
                    if (buffer.Any())
                    {
                        tokens.Add(buffer.AsString());
                    }

                    buffer.Clear();

                    var t = tokens.Last();
                    Expression leftOperand =
                        t == ")" ? null : Expression.Parameter(typeof(bool), tokens.Last());

                    tokens.Add(c.ToString());

                    var rightOperand = BootstrapExpr(q, eq, buffer, tokens, !Fop(tokens));

                    if (t == ")" && eq.Any())
                    {
                        leftOperand = eq.Pop();
                    }

                    if (!eq.Any() && q.Any() && q.First() != ')' && !eJs)
                    {
                        eq.Push(Expression.And(leftOperand!, rightOperand));
                    }
                    else
                    {
                        return Expression.And(leftOperand!, rightOperand);
                    }

                    break;
                }
                case '~':
                {
                    tokens.Add(c.ToString());
                    var operand = BootstrapExpr(q, eq, buffer, tokens, eJs);

                    if (!eq.Any() && q.Any() && q.First() != ')' && !eJs)
                    {
                        eq.Push(Expression.Not(operand));
                    }
                    else
                    {
                        return Expression.Not(operand);
                    }

                    break;
                }
                case '(':
                {
                    var lastToken = tokens.LastOrDefault();

                    tokens.Add(c.ToString());

                    if (lastToken != null && Operators.Concat(new[] {"("}).Contains(lastToken))
                    {
                        return BootstrapExpr(q, eq, buffer, tokens, lastToken != "(");
                    }

                    break;
                }
                case ')':
                {
                    if (buffer.Any())
                    {
                        tokens.Add(buffer.AsString());
                    }

                    var lastToken = tokens.LastOrDefault();

                    tokens.Add(c.ToString());
                    buffer.Clear();

                    return Expression.Parameter(typeof(bool), lastToken);
                }
                default: // Symbol
                {
                    buffer.Add(c);
                    break;
                }
            }
        }

        return Expression.Parameter(typeof(bool), buffer.Any() ? buffer.AsString() : tokens.Last());
    }

    private static bool Fop(IEnumerable<string> tokens)
    {
        var tokensList = tokens.ToList();
        return  tokensList.Count(t => t == "(") > tokensList.Count(t => t == ")");
    }

    private static IEnumerable<char> GiveToMeTheRestOfTheSymbol(IEnumerable<char> input)
    {
        return input.TakeWhile(c => !OperatorsRadix.Contains(c.ToString()) && !LegalTokens.Contains(c.ToString())).ToList();
    }

    private static IEnumerable<char> SetOperatorsPrecedenceWhenUndeclared(IList<char> s)
    {
        var input = new List<char>();

        if (s.ElementAt(0) != '(')
        {
            s.Insert(0, '(');
        }

        var p = 0;
        var op = false;

        for (var i = 0; i<s.Count;i++)
        {
            var c = s[i];

            if (c == '(')
            {
                p++;
                op = false;
            }
            else if (c == ')')
            {
                p--;
                op = false;
            }
            else if (OperatorsRadix.Contains(c.ToString()))
            {
                if (p == 0)
                {
                    input.Insert(0, '(');
                    p++;
                }

                op = true;

                if (c == '<')
                {
                    input.Add(c);
                    input.Add('=');
                    input.Add('>');
                    i += 2;
                    continue;
                }
                if (c == '=')
                {
                    input.Add(c);
                    input.Add('>');
                    i++;
                    continue;
                }
            }
            else if (p > 0 && op)
            {
                input.Add(c);
                var ros = GiveToMeTheRestOfTheSymbol(s.TakeLast(s.Count - (i + 1))).ToList();
                input.AddRange(ros);
                input.Add(')');
                p--;
                op = false;
                i += ros.Count;
                continue;
            }

            if (input.LastOrDefault() == ')' && c == ')')
            {
                p++;
                continue;
            }

            input.Add(c);
        }

        return input;
    }
}