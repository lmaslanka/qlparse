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
                if (IsStaticMethodInvocation())
                {
                    left = ParseStaticMethod(left);
                    continue;
                }

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

            if (_current.Kind == SyntaxKind.OpenBracket && DotBindingPower >= minBindingPower && IsMdarraySlice())
            {
                left = ParseMdarraySlice(left);
                continue;
            }

            if (_current.Kind == SyntaxKind.OpenBracket && DotBindingPower >= minBindingPower)
            {
                var openBracket = Advance();
                var index = ParseExpression();
                var closeBracket = Expect(SyntaxKind.CloseBracket);
                left = new JsonAccessorExpression
                {
                    Span = SourceSpan.From(left.Span, closeBracket),
                    Target = left,
                    OpenBracket = openBracket,
                    Index = index,
                    CloseBracket = closeBracket,
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
                if (_current.Kind == SyntaxKind.OpenParen)
                {
                    left = ParseMethodInvocation(left, dot, member);
                    continue;
                }

                left = new MemberAccessExpression
                {
                    Span = SourceSpan.From(left.Span, member),
                    Target = left,
                    Dot = dot,
                    Member = member,
                };
                continue;
            }

            if (_current.Kind == SyntaxKind.JsonArrowToken
                && ArrowBindingPower >= minBindingPower
                && NextKind == SyntaxKind.Identifier)
            {
                var arrow = Advance();
                var attribute = Expect(SyntaxKind.Identifier);
                left = new DereferenceExpression
                {
                    Span = SourceSpan.From(left.Span, attribute),
                    Reference = left,
                    Arrow = arrow,
                    Attribute = attribute,
                };
                continue;
            }

            var multisetPower = MultisetOpBindingPower();
            if (multisetPower != 0 && multisetPower >= minBindingPower)
            {
                left = ParseMultisetOp(left, multisetPower);
                continue;
            }

            if (ComparisonBindingPower >= minBindingPower && IsPeriodPredicate())
            {
                left = ParsePeriodPredicate(left);
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
                    case SyntaxKind.SimilarKeyword:
                        left = ParseSimilar(left);
                        continue;
                    case SyntaxKind.NotKeyword when NextKind == SyntaxKind.SimilarKeyword:
                        left = ParseSimilar(left);
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

    private MethodInvocationExpression ParseMethodInvocation(Expression target, SyntaxToken dot, SyntaxToken name)
    {
        ParseParenthesizedArguments(out var argumentsOpen, out var arguments, out var argumentsClose);
        return new MethodInvocationExpression
        {
            Span = SourceSpan.From(target.Span, argumentsClose),
            Target = target,
            Dot = dot,
            Name = name,
            ArgumentsOpen = argumentsOpen,
            Arguments = arguments,
            ArgumentsClose = argumentsClose,
        };
    }

    private StaticMethodInvocationExpression ParseStaticMethod(Expression type)
    {
        var doubleColon = Advance();
        var name = Expect(SyntaxKind.Identifier);
        ParseParenthesizedArguments(out var openParen, out var arguments, out var closeParen);
        return new StaticMethodInvocationExpression
        {
            Span = SourceSpan.From(type.Span, closeParen),
            Type = type,
            DoubleColon = doubleColon,
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    private bool IsStaticMethodInvocation()
    {
        var nameIndex = _index;
        if (nameIndex >= _tokens.Count || _tokens[nameIndex].Kind != SyntaxKind.Identifier)
        {
            return false;
        }

        var parenIndex = nameIndex + 1;
        if (parenIndex >= _tokens.Count || _tokens[parenIndex].Kind != SyntaxKind.OpenParen)
        {
            return false;
        }

        var first = parenIndex + 1;
        if (first >= _tokens.Count || _tokens[first].Kind == SyntaxKind.CloseParen)
        {
            return true;
        }

        if (_tokens[first].Kind != SyntaxKind.Number)
        {
            return true;
        }

        var afterNumber = first + 1;
        if (afterNumber >= _tokens.Count)
        {
            return true;
        }

        if (_tokens[afterNumber].Kind == SyntaxKind.CloseParen)
        {
            return false;
        }

        if (_tokens[afterNumber].Kind != SyntaxKind.Comma)
        {
            return true;
        }

        var scale = afterNumber + 1;
        var close = scale + 1;
        return scale >= _tokens.Count
            || _tokens[scale].Kind != SyntaxKind.Number
            || close >= _tokens.Count
            || _tokens[close].Kind != SyntaxKind.CloseParen;
    }

    private bool IsPeriodPredicate()
    {
        int rightIndex;
        if (IdentifierEquals(Keyword.Immediately))
        {
            if (!NextEquals(Keyword.Precedes) && !NextEquals(Keyword.Succeeds))
            {
                return false;
            }

            rightIndex = _index + 1;
        }
        else if (IdentifierEquals(Keyword.Equals)
            || IdentifierEquals(Keyword.Contains)
            || IdentifierEquals(Keyword.Precedes)
            || IdentifierEquals(Keyword.Succeeds))
        {
            rightIndex = _index;
        }
        else
        {
            return false;
        }

        return rightIndex < _tokens.Count && IsExpressionStart(_tokens[rightIndex]);
    }

    private static bool IsExpressionStart(SyntaxToken token) => token.Kind is
        SyntaxKind.Identifier
        or SyntaxKind.Number
        or SyntaxKind.String
        or SyntaxKind.OpenParen
        or SyntaxKind.PlusToken
        or SyntaxKind.MinusToken
        or SyntaxKind.NotKeyword
        or SyntaxKind.CaseKeyword
        or SyntaxKind.CastKeyword
        or SyntaxKind.TreatKeyword
        or SyntaxKind.DerefKeyword
        or SyntaxKind.SpecifictypeKeyword
        or SyntaxKind.NullIfKeyword
        or SyntaxKind.CoalesceKeyword
        or SyntaxKind.ArrayKeyword
        or SyntaxKind.MultisetKeyword
        or SyntaxKind.SetKeyword
        or SyntaxKind.ExistsKeyword
        or SyntaxKind.UniqueKeyword
        or SyntaxKind.TrueKeyword
        or SyntaxKind.FalseKeyword
        or SyntaxKind.NullKeyword
        or SyntaxKind.DateKeyword
        or SyntaxKind.TimeKeyword
        or SyntaxKind.TimestampKeyword
        or SyntaxKind.IntervalKeyword
        or SyntaxKind.QuestionMark
        or SyntaxKind.EmbeddedHost;

    private PeriodPredicateExpression ParsePeriodPredicate(Expression left)
    {
        SyntaxToken? immediately = null;
        if (IdentifierEquals(Keyword.Immediately))
        {
            immediately = Advance();
        }

        var operatorToken = Advance();
        var right = ParseExpression(ComparisonBindingPower + 1);
        return new PeriodPredicateExpression
        {
            Span = SourceSpan.From(left.Span, right.Span),
            Left = left,
            ImmediatelyKeyword = immediately,
            OperatorToken = operatorToken,
            Right = right,
        };
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

    private SimilarExpression ParseSimilar(Expression target)
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var similarKeyword = Expect(SyntaxKind.SimilarKeyword);
        if (!IdentifierEquals(Keyword.To))
        {
            throw new SqlParseException($"Expected TO, found {_current.Kind}", _current.Position);
        }

        var toKeyword = Advance();
        var pattern = ParseExpression(ComparisonBindingPower + 1);
        SyntaxToken? escapeKeyword = null;
        Expression? escape = null;
        if (_current.Kind == SyntaxKind.EscapeKeyword)
        {
            escapeKeyword = Advance();
            escape = ParseExpression(ComparisonBindingPower + 1);
        }

        return new SimilarExpression
        {
            Span = SourceSpan.From(target.Span, escape?.Span ?? pattern.Span),
            Target = target,
            NotKeyword = notKeyword,
            SimilarKeyword = similarKeyword,
            ToKeyword = toKeyword,
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

    private Expression ParseIs(Expression target)
    {
        var isKeyword = Expect(SyntaxKind.IsKeyword);
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        if (_current.Kind == SyntaxKind.DistinctKeyword)
        {
            return ParseDistinctFrom(target, isKeyword, notKeyword);
        }

        if (IsNormalizedPredicate())
        {
            return ParseNormalized(target, isKeyword, notKeyword);
        }

        if (IdentifierEquals(Keyword.Document) || IdentifierEquals(Keyword.Content) || IdentifierEquals(Keyword.Json))
        {
            return ParseMarkupPredicate(target, isKeyword, notKeyword);
        }

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

    private DistinctFromExpression ParseDistinctFrom(
        Expression target,
        SyntaxToken isKeyword,
        SyntaxToken? notKeyword)
    {
        var distinctKeyword = Expect(SyntaxKind.DistinctKeyword);
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var right = ParseExpression(ComparisonBindingPower + 1);
        return new DistinctFromExpression
        {
            Span = SourceSpan.From(target.Span, right.Span),
            Left = target,
            IsKeyword = isKeyword,
            NotKeyword = notKeyword,
            DistinctKeyword = distinctKeyword,
            FromKeyword = fromKeyword,
            Right = right,
        };
    }

    private bool IsNormalizedPredicate() =>
        IdentifierEquals(Keyword.Normalized) || (IsNormalizationForm() && NextIsNormalized());

    private bool IsNormalizationForm() =>
        IdentifierEquals(Keyword.Nfc)
        || IdentifierEquals(Keyword.Nfd)
        || IdentifierEquals(Keyword.Nfkc)
        || IdentifierEquals(Keyword.Nfkd);

    private bool NextIsNormalized() =>
        NextKind == SyntaxKind.Identifier
        && _index < _tokens.Count
        && TokenEquals(_tokens[_index], Keyword.Normalized);

    private NormalizedPredicateExpression ParseNormalized(
        Expression target,
        SyntaxToken isKeyword,
        SyntaxToken? notKeyword)
    {
        SyntaxToken? form = IsNormalizationForm() ? Advance() : null;
        if (!IdentifierEquals(Keyword.Normalized))
        {
            throw new SqlParseException($"Expected NORMALIZED, found {_current.Kind}", _current.Position);
        }

        var normalizedKeyword = Advance();
        return new NormalizedPredicateExpression
        {
            Span = SourceSpan.From(target.Span, normalizedKeyword),
            Target = target,
            IsKeyword = isKeyword,
            NotKeyword = notKeyword,
            Form = form,
            NormalizedKeyword = normalizedKeyword,
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

    private const int MultisetUnionBindingPower = 3;
    private const int MultisetIntersectBindingPower = 4;
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
