using System.Collections.Immutable;

namespace SqlParser;

internal sealed class Parser
{
    private readonly ImmutableArray<SyntaxToken> _tokens;
    private int _position;

    private Parser(ImmutableArray<SyntaxToken> tokens)
    {
        _tokens = tokens;
    }

    public static SelectStatement Parse(ImmutableArray<SyntaxToken> tokens)
    {
        var parser = new Parser(tokens);
        var statement = parser.ParseSelectStatement();
        parser.Expect(SyntaxKind.EndOfFile);
        return statement;
    }

    private SelectStatement ParseSelectStatement()
    {
        var selectKeyword = Expect(SyntaxKind.SelectKeyword);
        var selectList = ParseSelectList();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var tableName = Expect(SyntaxKind.Identifier);
        var where = Current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;

        return new SelectStatement
        {
            SelectKeyword = selectKeyword,
            SelectList = selectList,
            FromKeyword = fromKeyword,
            TableName = tableName,
            Where = where,
        };
    }

    private IReadOnlyList<Expression> ParseSelectList()
    {
        var items = new List<Expression> { ParseExpression() };
        while (Current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseExpression());
        }

        return items;
    }

    private WhereClause ParseWhereClause()
    {
        return new WhereClause
        {
            WhereKeyword = Expect(SyntaxKind.WhereKeyword),
            Expression = ParseExpression(),
        };
    }

    private Expression ParseExpression(int minBindingPower = 0)
    {
        var left = ParsePrefix();
        while (true)
        {
            var bindingPower = BindingPower(Current.Kind);
            if (bindingPower == 0 || bindingPower < minBindingPower)
            {
                break;
            }

            var operatorToken = Advance();
            var right = ParseExpression(bindingPower + 1);
            left = new BinaryExpression
            {
                Left = left,
                OperatorToken = operatorToken,
                Right = right,
            };
        }

        return left;
    }

    private Expression ParsePrefix()
    {
        return Current.Kind switch
        {
            SyntaxKind.Identifier => new IdentifierExpression { Identifier = Advance() },
            SyntaxKind.Number => new LiteralExpression { Literal = Advance() },
            _ => throw new SqlParseException($"Expected expression, found {Current.Kind}", Current.Position),
        };
    }

    private const int OrBindingPower = 1;
    private const int AndBindingPower = 2;
    private const int EqualsBindingPower = 3;

    private static int BindingPower(SyntaxKind kind) => kind switch
    {
        SyntaxKind.OrKeyword => OrBindingPower,
        SyntaxKind.AndKeyword => AndBindingPower,
        SyntaxKind.EqualsToken => EqualsBindingPower,
        _ => 0,
    };

    private SyntaxToken Current => _tokens[_position];

    private SyntaxToken Advance()
    {
        var current = Current;
        if (_position < _tokens.Length - 1)
        {
            _position++;
        }

        return current;
    }

    private SyntaxToken Expect(SyntaxKind kind)
    {
        if (Current.Kind != kind)
        {
            throw new SqlParseException($"Expected {kind}, found {Current.Kind}", Current.Position);
        }

        return Advance();
    }
}
