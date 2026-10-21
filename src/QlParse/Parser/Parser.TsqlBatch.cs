namespace QlParse;

// SQL Server scripts, batches and statement boundaries (workstream A).
// Statement boundaries: T-SQL terminators are optional, so a statement list is parsed as
//   repeat { skip any `;` terminators; stop unless IsTsqlStatementStart(); ParseStatement() }
// Every statement parser only has to stop at the first token it cannot use; it never looks for `;`.
// The list stops at the first token that cannot begin a statement (END, ELSE, GO, `)`, end of input) and
// leaves it to its caller, so BEGIN...END, TRY/CATCH and similar bodies reuse ParseTsqlStatementList()
// and then expect their own closing token; single-statement bodies (IF/ELSE, WHILE) call ParseStatement()
// and may skip a trailing `;` with SkipTsqlTerminators(). Implicit aliases never take a T-SQL reserved
// word (see IsPgReservedClauseWord), which is what lets `select a from t insert ...` split correctly.
// The list also enforces the three places T-SQL still wants a `;`: before WITH and THROW unless they open
// the list, and after MERGE.
internal sealed partial class Parser
{
    internal readonly record struct TsqlStatementList(
        List<Query> Statements,
        List<SyntaxToken>? Semicolons,
        SourceSpan? Span);

    // A whole input: one batch, or GO-separated batches. A single statement is returned as itself and a
    // GO-free input of several statements as a TsqlBatch; any GO line makes the root a TsqlScript.
    internal Query ParseTsqlScript()
    {
        var list = ParseTsqlStatementList();
        if (_current.Kind != SyntaxKind.BatchSeparator)
        {
            Expect(SyntaxKind.EndOfFile);
            return CollapseTsqlBatch(list);
        }

        var batches = new List<TsqlBatch>();
        while (_current.Kind == SyntaxKind.BatchSeparator)
        {
            batches.Add(BuildTsqlBatch(list, ParseTsqlBatchSeparator()));
            list = ParseTsqlStatementList();
        }

        Expect(SyntaxKind.EndOfFile);
        if (list.Span is not null)
        {
            batches.Add(BuildTsqlBatch(list, null));
        }

        return new TsqlScript
        {
            Span = SourceSpan.From(batches[0].Span, batches[^1].Span),
            Batches = batches,
        };
    }

    internal TsqlStatementList ParseTsqlStatementList()
    {
        var statements = new List<Query>();
        List<SyntaxToken>? semicolons = null;
        var start = _current;
        SourceSpan? end = null;
        var terminated = true;
        while (true)
        {
            while (_current.Kind == SyntaxKind.Semicolon)
            {
                var semicolon = Advance();
                (semicolons ??= []).Add(semicolon);
                end = semicolon.Span;
                terminated = true;
            }

            if (!IsTsqlStatementStart())
            {
                break;
            }

            if (!terminated && RequiresPrecedingTerminator())
            {
                throw new SqlParseException("The previous statement must be terminated with a semicolon", _current.Position);
            }

            var statement = ParseStatement();
            if (statement is MergeStatement && _current.Kind != SyntaxKind.Semicolon)
            {
                throw new SqlParseException("A MERGE statement must be terminated by a semicolon", _current.Position);
            }

            statements.Add(statement);
            end = statement.Span;
            terminated = false;
        }

        return new TsqlStatementList(statements, semicolons, end is null ? null : SourceSpan.From(start.Span, end.Value));
    }

    // Tokens that can begin a T-SQL statement. Verbs that are not reserved ISO keywords (INSERT, DECLARE,
    // BEGIN, IF, PRINT, EXEC, ...) and labels all arrive as identifiers.
    internal bool IsTsqlStatementStart() => _current.Kind is SyntaxKind.Identifier
        or SyntaxKind.SelectKeyword
        or SyntaxKind.WithKeyword
        or SyntaxKind.UpdateKeyword
        or SyntaxKind.SetKeyword
        or SyntaxKind.FetchKeyword
        or SyntaxKind.OpenParen
        || IsTsqlEndConversation();

    internal void SkipTsqlTerminators()
    {
        while (_current.Kind == SyntaxKind.Semicolon)
        {
            Advance();
        }
    }

    private bool RequiresPrecedingTerminator() =>
        _current.Kind == SyntaxKind.WithKeyword || IdentifierEquals(Keyword.TsqlThrow);

    private Query CollapseTsqlBatch(TsqlStatementList list)
    {
        if (list.Statements.Count == 0)
        {
            throw new SqlParseException($"Expected statement, found {_current.Kind}", _current.Position);
        }

        return list.Statements.Count == 1 ? list.Statements[0] : BuildTsqlBatch(list, null);
    }

    private static TsqlBatch BuildTsqlBatch(TsqlStatementList list, TsqlBatchSeparator? separator)
    {
        SourceSpan span;
        if (list.Span is null)
        {
            span = separator!.Span;
        }
        else
        {
            span = separator is null ? list.Span.Value : SourceSpan.From(list.Span.Value, separator.Span);
        }

        return new TsqlBatch
        {
            Span = span,
            Statements = list.Statements,
            Semicolons = list.Semicolons ?? (IReadOnlyList<SyntaxToken>)[],
            Separator = separator,
        };
    }

    // The lexer only produces BatchSeparator for a GO alone on its line, so a number right after it on the
    // same line is its repeat count.
    private TsqlBatchSeparator ParseTsqlBatchSeparator()
    {
        var go = Advance();
        SyntaxToken? count = null;
        if (_current.Kind == SyntaxKind.Number && !_source.AsSpan(go.Span.End, _current.Position - go.Span.End).ContainsAny('\n', '\r'))
        {
            count = Advance();
        }

        return new TsqlBatchSeparator
        {
            Span = count is null ? go.Span : SourceSpan.From(go, count.Value),
            GoKeyword = go,
            Count = count,
        };
    }
}
