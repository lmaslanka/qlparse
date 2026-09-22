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
                var collateKeyword = Advance();
                var name = Expect(SyntaxKind.Identifier);
                left = new CollateExpression
                {
                    Span = SourceSpan.From(left.Span, name),
                    Expression = left,
                    CollateKeyword = collateKeyword,
                    Name = name,
                };
                continue;
            }

            if (_current.Kind == SyntaxKind.DoubleColonToken && ColonCastBindingPower >= minBindingPower)
            {
                var doubleColon = Advance();
                var type = ParseDataType();
                left = new ColonCastExpression
                {
                    Span = SourceSpan.From(left.Span, type.Span),
                    Expression = left,
                    DoubleColon = doubleColon,
                    Type = type,
                };
                continue;
            }

            if (_current.Kind == SyntaxKind.Dot && DotBindingPower >= minBindingPower)
            {
                var dot = Advance();
                if (_current.Kind == SyntaxKind.Star)
                {
                    var star = Advance();
                    left = new QualifiedStarExpression
                    {
                        Span = SourceSpan.From(left.Span, star),
                        Target = left,
                        Dot = dot,
                        Star = star,
                    };
                    continue;
                }

                var member = Expect(SyntaxKind.Identifier);
                left = new MemberAccessExpression
                {
                    Span = SourceSpan.From(left.Span, member),
                    Target = left,
                    Dot = dot,
                    Member = member,
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
                Span = SourceSpan.From(left.Span, right.Span),
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
            Span = SourceSpan.From(target.Span, escape?.Span ?? pattern.Span),
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
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var betweenKeyword = Expect(SyntaxKind.BetweenKeyword);
        var lower = ParseExpression(ComparisonBindingPower + 1);
        var andKeyword = Expect(SyntaxKind.AndKeyword);
        var upper = ParseExpression(ComparisonBindingPower + 1);
        return new BetweenExpression
        {
            Span = SourceSpan.From(target.Span, upper.Span),
            Target = target,
            NotKeyword = notKeyword,
            BetweenKeyword = betweenKeyword,
            Lower = lower,
            AndKeyword = andKeyword,
            Upper = upper,
        };
    }

    private InExpression ParseIn(Expression target)
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        if (IsQueryStart(_current.Kind))
        {
            var query = ParseQuery();
            var closeParen = Expect(SyntaxKind.CloseParen);
            return new InExpression
            {
                Span = SourceSpan.From(target.Span, closeParen),
                Target = target,
                NotKeyword = notKeyword,
                InKeyword = inKeyword,
                OpenParen = openParen,
                Values = [],
                Query = query,
                CloseParen = closeParen,
            };
        }

        var values = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            values.Add(ParseExpression());
        }

        var close = Expect(SyntaxKind.CloseParen);
        return new InExpression
        {
            Span = SourceSpan.From(target.Span, close),
            Target = target,
            NotKeyword = notKeyword,
            InKeyword = inKeyword,
            OpenParen = openParen,
            Values = values,
            CloseParen = close,
        };
    }

    private IsExpression ParseIs(Expression target)
    {
        var isKeyword = Expect(SyntaxKind.IsKeyword);
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var value = ParseIsValue();
        return new IsExpression
        {
            Span = SourceSpan.From(target.Span, value),
            Target = target,
            IsKeyword = isKeyword,
            NotKeyword = notKeyword,
            Value = value,
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
