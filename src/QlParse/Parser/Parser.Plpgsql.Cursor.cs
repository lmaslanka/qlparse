namespace QlParse;

// PL/pgSQL cursor commands: OPEN (bound, unbound FOR query, FOR EXECUTE), FETCH and MOVE.
// CLOSE has the ISO shape and is left to the regular statement parser.
internal sealed partial class Parser
{
    // OPEN cursor [[NO] SCROLL] FOR {query | EXECUTE command [USING ...]} | OPEN cursor [(arguments)]
    private PlpgsqlOpenStatement ParsePlpgsqlOpen()
    {
        var openKeyword = Advance();
        var cursor = Expect(SyntaxKind.Identifier);
        SyntaxToken? noKeyword = IdentifierEquals(Keyword.No) && NextEquals(Keyword.Scroll) ? Advance() : null;
        SyntaxToken? scrollKeyword = IdentifierEquals(Keyword.Scroll) ? Advance() : null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            var forKeyword = Advance();
            var query = IdentifierEquals(Keyword.Execute) ? ParsePlpgsqlExecute() : ParseQuery();
            return new PlpgsqlOpenStatement
            {
                Span = SourceSpan.From(openKeyword, query.Span),
                OpenKeyword = openKeyword,
                Cursor = cursor,
                NoKeyword = noKeyword,
                ScrollKeyword = scrollKeyword,
                ForKeyword = forKeyword,
                Query = query,
            };
        }

        SyntaxToken? openParen = _current.Kind == SyntaxKind.OpenParen ? Advance() : null;
        IReadOnlyList<Expression> arguments = openParen is null ? [] : ParsePgFunctionArguments();
        SyntaxToken? closeParen = openParen is null ? null : Expect(SyntaxKind.CloseParen);
        return new PlpgsqlOpenStatement
        {
            Span = SourceSpan.From(openKeyword, PgPreviousToken()),
            OpenKeyword = openKeyword,
            Cursor = cursor,
            NoKeyword = noKeyword,
            ScrollKeyword = scrollKeyword,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    // FETCH [direction [FROM | IN]] cursor INTO targets | MOVE [direction [FROM | IN]] cursor
    private PlpgsqlFetchStatement ParsePlpgsqlFetch()
    {
        var keyword = Advance();
        var direction = ParsePlpgsqlFetchDirection();
        SyntaxToken? fromKeyword = _current.Kind is SyntaxKind.FromKeyword or SyntaxKind.InKeyword ? Advance() : null;
        var cursor = Expect(SyntaxKind.Identifier);
        var into = TryParsePlpgsqlInto();
        return new PlpgsqlFetchStatement
        {
            Span = SourceSpan.From(keyword, PgPreviousToken()),
            Keyword = keyword,
            Direction = direction.Words,
            Count = direction.Count,
            FromKeyword = fromKeyword,
            Cursor = cursor,
            IntoKeyword = into.IntoKeyword,
            Targets = into.Targets ?? [],
        };
    }

    // A count binds tighter than IN, so `MOVE FORWARD 2 IN c` does not read `2 IN c` as a predicate.
    private Expression ParsePlpgsqlFetchCount() => ParseExpression(ComparisonBindingPower + 1);

    private readonly record struct PlpgsqlFetchDirection(IReadOnlyList<SyntaxToken> Words, Expression? Count);

    // NEXT | PRIOR | FIRST | LAST | ABSOLUTE n | RELATIVE n | n | ALL | FORWARD [n | ALL] | BACKWARD [n | ALL]
    private PlpgsqlFetchDirection ParsePlpgsqlFetchDirection()
    {
        if (IsPlpgsqlFetchCountStart())
        {
            return new PlpgsqlFetchDirection([], ParsePlpgsqlFetchCount());
        }

        if (_current.Kind == SyntaxKind.AllKeyword || IsPlpgsqlFetchPositionWord())
        {
            return new PlpgsqlFetchDirection([Advance()], null);
        }

        if (IdentifierEquals(Keyword.Absolute) || IdentifierEquals(Keyword.Relative))
        {
            var word = Advance();
            return new PlpgsqlFetchDirection([word], ParsePlpgsqlFetchCount());
        }

        return IdentifierEquals(Keyword.PlpgsqlForward) || IdentifierEquals(Keyword.PlpgsqlBackward)
            ? ParsePlpgsqlFetchStep()
            : new PlpgsqlFetchDirection([], null);
    }

    private bool IsPlpgsqlFetchCountStart() => _current.Kind is SyntaxKind.Number or SyntaxKind.MinusToken;

    private bool IsPlpgsqlFetchPositionWord() =>
        IdentifierEquals(Keyword.Next) || IdentifierEquals(Keyword.Prior) || IdentifierEquals(Keyword.First) || IdentifierEquals(Keyword.Last);

    // FORWARD | BACKWARD, then an optional count or ALL.
    private PlpgsqlFetchDirection ParsePlpgsqlFetchStep()
    {
        var direction = Advance();
        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            return new PlpgsqlFetchDirection([direction, Advance()], null);
        }

        return new PlpgsqlFetchDirection([direction], IsPlpgsqlFetchCountStart() ? ParsePlpgsqlFetchCount() : null);
    }
}
