namespace QlParse;

internal sealed partial class Parser
{
    private IReadOnlyList<Expression> ParseExpressionList()
    {
        var items = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseExpression());
        }

        return items;
    }

    private Expression ParseExpression(int minBindingPower = 0)
    {
        var left = ParsePrefix();
        while (true)
        {
            if (_current.Kind == SyntaxKind.CollateKeyword && CollateBindingPower >= minBindingPower)
            {
                left = new CollateExpression
                {
                    Expression = left,
                    CollateKeyword = Advance(),
                    Name = Expect(SyntaxKind.Identifier),
                };
                continue;
            }

            if (_current.Kind == SyntaxKind.DoubleColonToken && ColonCastBindingPower >= minBindingPower)
            {
                var doubleColon = Advance();
                left = new ColonCastExpression
                {
                    Expression = left,
                    DoubleColon = doubleColon,
                    Type = ParseDataType(),
                };
                continue;
            }

            if (_current.Kind == SyntaxKind.Dot && DotBindingPower >= minBindingPower)
            {
                var dot = Advance();
                if (_current.Kind == SyntaxKind.Star)
                {
                    left = new QualifiedStarExpression
                    {
                        Target = left,
                        Dot = dot,
                        Star = Advance(),
                    };
                    continue;
                }

                left = new MemberAccessExpression
                {
                    Target = left,
                    Dot = dot,
                    Member = Expect(SyntaxKind.Identifier),
                };
                continue;
            }

            if (ComparisonBindingPower >= minBindingPower)
            {
                switch (_current.Kind)
                {
                    case SyntaxKind.BetweenKeyword:
                        left = ParseBetween(left);
                        continue;
                    case SyntaxKind.NotKeyword when NextKind == SyntaxKind.BetweenKeyword:
                        left = ParseBetween(left);
                        continue;
                    case SyntaxKind.InKeyword:
                        left = ParseIn(left);
                        continue;
                    case SyntaxKind.NotKeyword when NextKind == SyntaxKind.InKeyword:
                        left = ParseIn(left);
                        continue;
                    case SyntaxKind.LikeKeyword:
                        left = ParseLike(left);
                        continue;
                    case SyntaxKind.NotKeyword when NextKind == SyntaxKind.LikeKeyword:
                        left = ParseLike(left);
                        continue;
                    case SyntaxKind.IsKeyword:
                        left = ParseIs(left);
                        continue;
                    case SyntaxKind.OverlapsKeyword:
                        left = ParseOverlaps(left);
                        continue;
                    case SyntaxKind.MatchKeyword:
                        left = ParseMatch(left);
                        continue;
                }
            }

            var bindingPower = BindingPower(_current.Kind);
            if (bindingPower == 0 || bindingPower < minBindingPower)
            {
                break;
            }

            var operatorToken = Advance();
            if (bindingPower == ComparisonBindingPower
                && _current.Kind is SyntaxKind.AllKeyword or SyntaxKind.AnyKeyword or SyntaxKind.SomeKeyword)
            {
                left = ParseQuantifiedSubquery(left, operatorToken);
                continue;
            }

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

    private LikeExpression ParseLike(Expression target)
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var likeKeyword = Expect(SyntaxKind.LikeKeyword);
        var pattern = ParseExpression(ComparisonBindingPower + 1);
        SyntaxToken? escapeKeyword = null;
        Expression? escape = null;
        if (_current.Kind == SyntaxKind.EscapeKeyword)
        {
            escapeKeyword = Advance();
            escape = ParseExpression(ComparisonBindingPower + 1);
        }

        return new LikeExpression
        {
            Target = target,
            NotKeyword = notKeyword,
            LikeKeyword = likeKeyword,
            Pattern = pattern,
            EscapeKeyword = escapeKeyword,
            Escape = escape,
        };
    }

    private BetweenExpression ParseBetween(Expression target)
    {
        return new BetweenExpression
        {
            Target = target,
            NotKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null,
            BetweenKeyword = Expect(SyntaxKind.BetweenKeyword),
            Lower = ParseExpression(ComparisonBindingPower + 1),
            AndKeyword = Expect(SyntaxKind.AndKeyword),
            Upper = ParseExpression(ComparisonBindingPower + 1),
        };
    }

    private InExpression ParseIn(Expression target)
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        if (IsQueryStart(_current.Kind))
        {
            return new InExpression
            {
                Target = target,
                NotKeyword = notKeyword,
                InKeyword = inKeyword,
                OpenParen = openParen,
                Values = [],
                Query = ParseQuery(),
                CloseParen = Expect(SyntaxKind.CloseParen),
            };
        }

        var values = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            values.Add(ParseExpression());
        }

        return new InExpression
        {
            Target = target,
            NotKeyword = notKeyword,
            InKeyword = inKeyword,
            OpenParen = openParen,
            Values = values,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private IsExpression ParseIs(Expression target)
    {
        return new IsExpression
        {
            Target = target,
            IsKeyword = Expect(SyntaxKind.IsKeyword),
            NotKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null,
            Value = ParseIsValue(),
        };
    }

    private SyntaxToken ParseIsValue()
    {
        if (_current.Kind is SyntaxKind.NullKeyword or SyntaxKind.TrueKeyword
            or SyntaxKind.FalseKeyword or SyntaxKind.UnknownKeyword)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected NULL, TRUE, FALSE, or UNKNOWN, found {_current.Kind}", _current.Position);
    }

    private const int OrBindingPower = 1;
    private const int AndBindingPower = 2;
    private const int NotBindingPower = 3;
    private const int ComparisonBindingPower = 4;
    private const int AddBindingPower = 5;
    private const int MultiplyBindingPower = 6;
    private const int UnaryBindingPower = 7;
    private const int DotBindingPower = 8;
    private const int ArrowBindingPower = 9;
    private const int ColonCastBindingPower = 10;
    private const int CollateBindingPower = 11;

    private static int BindingPower(SyntaxKind kind) => kind switch
    {
        SyntaxKind.OrKeyword => OrBindingPower,
        SyntaxKind.AndKeyword => AndBindingPower,
        SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or SyntaxKind.GreaterThan
            or SyntaxKind.GreaterOrEqual or SyntaxKind.LessThan or SyntaxKind.LessOrEqual
            => ComparisonBindingPower,
        SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.ConcatToken => AddBindingPower,
        SyntaxKind.Star or SyntaxKind.SlashToken => MultiplyBindingPower,
        SyntaxKind.JsonArrowToken or SyntaxKind.JsonTextArrowToken => ArrowBindingPower,
        _ => 0,
    };
}
