namespace QlParse;

// PL/pgSQL statement dispatch. PL/pgSQL statements are recognised by their first word; anything else is
// an SQL statement handed to the regular statement parser, after its INTO clause (if any) is cut out.
internal sealed partial class Parser
{
    private readonly record struct PlpgsqlIntoScan(int IntoIndex, int EndIndex);

    private Query ParsePlpgsqlStatement()
    {
        if (IsPlpgsqlLabelStart())
        {
            return ParsePlpgsqlLabeled(TryParsePlpgsqlLabel()!);
        }

        return _current.Kind switch
        {
            SyntaxKind.Identifier => TryParsePlpgsqlIdentifierStatement() ?? ParsePlpgsqlSql(),
            SyntaxKind.ForKeyword => ParsePlpgsqlFor(null),
            SyntaxKind.CaseKeyword => ParsePlpgsqlCase(),
            SyntaxKind.FetchKeyword => ParsePlpgsqlFetch(),
            SyntaxKind.NullKeyword when NextKind == SyntaxKind.Semicolon => ParsePlpgsqlNull(),
            _ => ParsePlpgsqlSql(),
        };
    }

    private Query ParsePlpgsqlLabeled(PlpgsqlLabel label)
    {
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            return ParsePlpgsqlFor(label);
        }

        if (IdentifierEquals(Keyword.Declare) || IdentifierEquals(Keyword.Begin))
        {
            return ParsePlpgsqlBlock(label);
        }

        if (IdentifierEquals(Keyword.Loop))
        {
            return ParsePlpgsqlLoop(label);
        }

        if (IdentifierEquals(Keyword.While))
        {
            return ParsePlpgsqlWhile(label);
        }

        return IdentifierEquals(Keyword.PlpgsqlForeach)
            ? ParsePlpgsqlForeach(label)
            : throw new SqlParseException($"Expected block or loop after label, found {_current.Kind}", _current.Position);
    }

    private Query? TryParsePlpgsqlIdentifierStatement()
    {
        if (IsPlpgsqlAssignment())
        {
            return ParsePlpgsqlAssignment();
        }

        return _current.Length switch
        {
            Len2 => IdentifierEquals(Keyword.If) ? ParsePlpgsqlIf() : null,
            Len3 => IdentifierEquals(Keyword.Get) ? ParsePlpgsqlGetDiagnostics() : null,
            Len4 => TryParsePlpgsqlLen4(),
            Len5 => TryParsePlpgsqlLen5(),
            Len6 => TryParsePlpgsqlLen6(),
            Len7 => TryParsePlpgsqlLen7(),
            Len8 => IdentifierEquals(Keyword.Continue) ? ParsePlpgsqlExit() : null,
            _ => null,
        };
    }

    private Query? TryParsePlpgsqlLen4()
    {
        if (IdentifierEquals(Keyword.Loop))
        {
            return ParsePlpgsqlLoop(null);
        }

        if (IdentifierEquals(Keyword.Exit))
        {
            return ParsePlpgsqlExit();
        }

        if (IdentifierEquals(Keyword.PlpgsqlMove))
        {
            return ParsePlpgsqlFetch();
        }

        return IdentifierEquals(Keyword.Open) ? ParsePlpgsqlOpen() : null;
    }

    private Query? TryParsePlpgsqlLen5()
    {
        if (IdentifierEquals(Keyword.Begin))
        {
            return ParsePlpgsqlBlock(null);
        }

        if (IdentifierEquals(Keyword.While))
        {
            return ParsePlpgsqlWhile(null);
        }

        return IdentifierEquals(Keyword.PlpgsqlRaise) ? ParsePlpgsqlRaise() : null;
    }

    private Query? TryParsePlpgsqlLen6()
    {
        if (IdentifierEquals(Keyword.Return))
        {
            return ParsePlpgsqlReturn();
        }

        return IdentifierEquals(Keyword.PlpgsqlAssert) ? ParsePlpgsqlAssert() : null;
    }

    private Query? TryParsePlpgsqlLen7()
    {
        if (IdentifierEquals(Keyword.Declare))
        {
            return ParsePlpgsqlBlock(null);
        }

        if (IdentifierEquals(Keyword.PlpgsqlPerform))
        {
            return ParsePlpgsqlPerform();
        }

        if (IdentifierEquals(Keyword.PlpgsqlForeach))
        {
            return ParsePlpgsqlForeach(null);
        }

        return IdentifierEquals(Keyword.Execute) ? ParsePlpgsqlExecute() : null;
    }

    private PlpgsqlNullStatement ParsePlpgsqlNull()
    {
        var nullKeyword = Advance();
        return new PlpgsqlNullStatement { Span = nullKeyword.Span, NullKeyword = nullKeyword };
    }

    // target := expr, or target = expr; the target is a variable, a record field or an array element.
    private bool IsPlpgsqlAssignment()
    {
        var index = _index;
        while (index < _tokens.Count)
        {
            var kind = _tokens[index].Kind;
            if (kind == SyntaxKind.Dot && index + 1 < _tokens.Count && _tokens[index + 1].Kind == SyntaxKind.Identifier)
            {
                index += Len2;
                continue;
            }

            if (kind != SyntaxKind.OpenBracket)
            {
                return kind is SyntaxKind.ColonEqualsToken or SyntaxKind.EqualsToken;
            }

            index = SkipPlpgsqlBrackets(index);
        }

        return false;
    }

    // The index just past the bracket that closes the one at `index`, or the token count.
    private int SkipPlpgsqlBrackets(int index)
    {
        var depth = 0;
        while (index < _tokens.Count)
        {
            var kind = _tokens[index].Kind;
            index++;
            if (kind == SyntaxKind.OpenBracket)
            {
                depth++;
                continue;
            }

            if (kind == SyntaxKind.CloseBracket)
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }

                continue;
            }

            if (kind is SyntaxKind.Semicolon or SyntaxKind.EndOfFile)
            {
                return _tokens.Count;
            }
        }

        return index;
    }

    private PlpgsqlAssignment ParsePlpgsqlAssignment()
    {
        var target = ParsePlpgsqlTarget();
        var assignment = Advance();
        var value = ParseExpression();
        return new PlpgsqlAssignment
        {
            Span = SourceSpan.From(target.Span, value.Span),
            Target = target,
            AssignmentToken = assignment,
            Value = value,
        };
    }

    // A variable, record field or array element: member access and subscripts only, no operators.
    private Expression ParsePlpgsqlTarget() => ParseExpression(DotBindingPower);

    private List<Expression> ParsePlpgsqlTargets()
    {
        var targets = new List<Expression> { ParsePlpgsqlTarget() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            targets.Add(ParsePlpgsqlTarget());
        }

        return targets;
    }

    private void SeekPlpgsql(int index)
    {
        _current = _tokens[index];
        _index = index + 1;
    }

    // An SQL statement. `INTO [STRICT] targets` may appear anywhere in it (SELECT a INTO x FROM t,
    // INSERT ... RETURNING id INTO x); when present it is cut out and the remaining tokens are parsed
    // by a nested parser, so the SQL grammar never sees it.
    private Query ParsePlpgsqlSql()
    {
        var start = _index - 1;
        var scan = IdentifierEquals(Keyword.PgImport) ? new PlpgsqlIntoScan(-1, start) : ScanPlpgsqlInto(start);
        if (scan.IntoIndex < 0)
        {
            return ParseStatement();
        }

        SeekPlpgsql(scan.IntoIndex);
        var intoKeyword = Advance();
        SyntaxToken? strictKeyword = IdentifierEquals(Keyword.RoutineStrict) ? Advance() : null;
        var targets = ParsePlpgsqlTargets();
        var statement = ParsePlpgsqlSubStatement(start, scan.IntoIndex, _index - 1, scan.EndIndex);
        SeekPlpgsql(scan.EndIndex);
        return new PlpgsqlSqlStatement
        {
            Span = SourceSpan.From(_tokens[start], PgPreviousToken()),
            Statement = statement,
            IntoKeyword = intoKeyword,
            StrictKeyword = strictKeyword,
            Targets = targets,
        };
    }

    // Finds the statement's INTO at parenthesis depth 0, skipping INSERT INTO and MERGE INTO, and the
    // semicolon that ends the statement. IMPORT FOREIGN SCHEMA ... INTO is never scanned.
    private PlpgsqlIntoScan ScanPlpgsqlInto(int start)
    {
        var into = -1;
        var depth = 0;
        var index = start;
        while (index < _tokens.Count)
        {
            var token = _tokens[index];
            if (token.Kind is SyntaxKind.Semicolon or SyntaxKind.EndOfFile && depth == 0)
            {
                break;
            }

            depth += PlpgsqlDepthChange(token.Kind);
            if (into < 0 && depth == 0 && TokenEquals(token, Keyword.Into) && !IsPlpgsqlVerbInto(index))
            {
                into = index;
            }

            index++;
        }

        return new PlpgsqlIntoScan(into, index);
    }

    private static int PlpgsqlDepthChange(SyntaxKind kind) => kind switch
    {
        SyntaxKind.OpenParen or SyntaxKind.OpenBracket => 1,
        SyntaxKind.CloseParen or SyntaxKind.CloseBracket => -1,
        _ => 0,
    };

    private bool IsPlpgsqlVerbInto(int index) =>
        index > 0 && (TokenEquals(_tokens[index - 1], Keyword.Insert) || TokenEquals(_tokens[index - 1], Keyword.Merge));

    // Parses tokens [start, intoStart) + [intoEnd, end) as one statement, ended by a synthetic end-of-file
    // token at the position of the statement's terminator.
    private Query ParsePlpgsqlSubStatement(int start, int intoStart, int intoEnd, int end)
    {
        var tokens = new List<SyntaxToken>(end - start + 1);
        for (var index = start; index < intoStart; index++)
        {
            tokens.Add(_tokens[index]);
        }

        for (var index = intoEnd; index < end; index++)
        {
            tokens.Add(_tokens[index]);
        }

        return ParsePlpgsqlTokens(tokens, end, query: false);
    }

    // Parses tokens [_index - 1, end) as one query (a FOR loop query that ends at LOOP), then moves to `end`.
    private Query ParsePlpgsqlBoundedQuery(int end)
    {
        var start = _index - 1;
        var tokens = new List<SyntaxToken>(end - start + 1);
        for (var index = start; index < end; index++)
        {
            tokens.Add(_tokens[index]);
        }

        var query = ParsePlpgsqlTokens(tokens, end, query: true);
        SeekPlpgsql(end);
        return query;
    }

    private Query ParsePlpgsqlTokens(List<SyntaxToken> tokens, int end, bool query)
    {
        var terminator = _tokens[end];
        tokens.Add(new SyntaxToken(SyntaxKind.EndOfFile, terminator.Position, 0, terminator.LeadingTriviaStart, 0));
        var parser = new Parser(tokens, _source, _flags);
        var result = query ? parser.ParseQuery() : parser.ParseStatement();
        parser.Expect(SyntaxKind.EndOfFile);
        return result;
    }
}
