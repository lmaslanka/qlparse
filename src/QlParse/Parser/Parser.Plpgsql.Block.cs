namespace QlParse;

// PL/pgSQL blocks: [<<label>>] [DECLARE declarations] BEGIN statements [EXCEPTION handlers] END [label].
// These methods only run on a nested parser over the tokens of a routine body (see Parser.PgRoutine.Body).
internal sealed partial class Parser
{
    private const int LabelTokenLength = 2;

    internal PlpgsqlBlock ParsePlpgsqlBody()
    {
        var label = TryParsePlpgsqlLabel();
        var block = ParsePlpgsqlBlock(label);
        if (_current.Kind == SyntaxKind.Semicolon)
        {
            Advance();
        }

        Expect(SyntaxKind.EndOfFile);
        return block;
    }

    private PlpgsqlBlock ParsePlpgsqlBlock(PlpgsqlLabel? label)
    {
        var start = _current;
        SyntaxToken? declareKeyword = IdentifierEquals(Keyword.Declare) ? Advance() : null;
        IReadOnlyList<Query> declarations = declareKeyword is null ? [] : ParsePlpgsqlDeclarations();
        var beginKeyword = ExpectIdent(Keyword.Begin);
        var statements = ParsePlpgsqlStatements();
        SyntaxToken? exceptionKeyword = IdentifierEquals(Keyword.Exception) ? Advance() : null;
        IReadOnlyList<PlpgsqlExceptionHandler> handlers = exceptionKeyword is null ? [] : ParsePlpgsqlExceptionHandlers();
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endLabel = TryParsePlpgsqlEndLabel();
        return new PlpgsqlBlock
        {
            Span = label is null ? SourceSpan.From(start, PgPreviousToken()) : SourceSpan.From(label.Span, PgPreviousToken()),
            Label = label,
            DeclareKeyword = declareKeyword,
            Declarations = declarations,
            BeginKeyword = beginKeyword,
            Statements = statements,
            ExceptionKeyword = exceptionKeyword,
            Handlers = handlers,
            EndKeyword = endKeyword,
            EndLabel = endLabel,
        };
    }

    private bool IsPlpgsqlLabelStart() =>
        _current.Kind == SyntaxKind.OperatorToken && _current.Length == LabelTokenLength
        && _source[_current.Position] == '<' && _source[_current.Position + 1] == '<';

    private PlpgsqlLabel? TryParsePlpgsqlLabel()
    {
        if (!IsPlpgsqlLabelStart())
        {
            return null;
        }

        var openToken = Advance();
        var name = ExpectPgWord();
        if (_current.Kind != SyntaxKind.OperatorToken || _current.Length != LabelTokenLength || _source[_current.Position] != '>')
        {
            throw new SqlParseException($"Expected >>, found {_current.Kind}", _current.Position);
        }

        var closeToken = Advance();
        return new PlpgsqlLabel { Span = SourceSpan.From(openToken, closeToken), OpenToken = openToken, Name = name, CloseToken = closeToken };
    }

    // A label after END or END LOOP; a label may be any word, so only the terminator ends the statement.
    private SyntaxToken? TryParsePlpgsqlEndLabel() =>
        IsPgWordKind(_current.Kind) && _current.Kind != SyntaxKind.WhenKeyword ? Advance() : null;

    // Declarations up to BEGIN; a repeated DECLARE keyword between declarations is allowed and skipped.
    private List<Query> ParsePlpgsqlDeclarations()
    {
        var declarations = new List<Query>();
        while (!IdentifierEquals(Keyword.Begin) && _current.Kind != SyntaxKind.EndOfFile)
        {
            if (IdentifierEquals(Keyword.Declare))
            {
                Advance();
                continue;
            }

            declarations.Add(ParsePlpgsqlDeclaration());
            Expect(SyntaxKind.Semicolon);
        }

        return declarations;
    }

    private Query ParsePlpgsqlDeclaration()
    {
        var name = Expect(SyntaxKind.Identifier);
        if (IdentifierEquals(Keyword.PlpgsqlAlias) && NextKind == SyntaxKind.ForKeyword)
        {
            var aliasKeyword = Advance();
            var forKeyword = Advance();
            var target = _current.Kind is SyntaxKind.PositionalParameter or SyntaxKind.Identifier
                ? Advance()
                : throw new SqlParseException($"Expected parameter, found {_current.Kind}", _current.Position);
            return new PlpgsqlAliasDeclaration
            {
                Span = SourceSpan.From(name, target),
                Name = name,
                AliasKeyword = aliasKeyword,
                ForKeyword = forKeyword,
                Target = target,
            };
        }

        return IsPlpgsqlCursorDeclaration() ? ParsePlpgsqlCursorDeclaration(name) : ParsePlpgsqlVariableDeclaration(name);
    }

    private bool IsPlpgsqlCursorDeclaration() =>
        IdentifierEquals(Keyword.Cursor)
        || (IdentifierEquals(Keyword.Scroll) && NextEquals(Keyword.Cursor))
        || (IdentifierEquals(Keyword.No) && NextEquals(Keyword.Scroll));

    // name [[NO] SCROLL] CURSOR [(arguments)] {FOR | IS} query
    private PlpgsqlCursorDeclaration ParsePlpgsqlCursorDeclaration(SyntaxToken name)
    {
        SyntaxToken? noKeyword = IdentifierEquals(Keyword.No) ? Advance() : null;
        SyntaxToken? scrollKeyword = IdentifierEquals(Keyword.Scroll) ? Advance() : null;
        var cursorKeyword = ExpectIdent(Keyword.Cursor);
        PgParameterList? arguments = _current.Kind == SyntaxKind.OpenParen ? ParsePgParameterList() : null;
        var forKeyword = _current.Kind is SyntaxKind.ForKeyword or SyntaxKind.IsKeyword
            ? Advance()
            : throw new SqlParseException($"Expected FOR or IS, found {_current.Kind}", _current.Position);
        var query = ParseQuery();
        return new PlpgsqlCursorDeclaration
        {
            Span = SourceSpan.From(name, query.Span),
            Name = name,
            NoKeyword = noKeyword,
            ScrollKeyword = scrollKeyword,
            CursorKeyword = cursorKeyword,
            OpenParen = arguments?.OpenParen,
            Arguments = arguments?.Parameters ?? [],
            CloseParen = arguments?.CloseParen,
            ForKeyword = forKeyword,
            Query = query,
        };
    }

    // name [CONSTANT] type [%TYPE | %ROWTYPE] [COLLATE c] [NOT NULL] [{DEFAULT | := | =} expr]
    private PlpgsqlVariableDeclaration ParsePlpgsqlVariableDeclaration(SyntaxToken name)
    {
        SyntaxToken? constantKeyword = IdentifierEquals(Keyword.PlpgsqlConstant) ? Advance() : null;
        var type = ParseDataType();
        var typeAttribute = TryParsePgTypeAttribute();
        SyntaxToken? collateKeyword = _current.Kind == SyntaxKind.CollateKeyword ? Advance() : null;
        IReadOnlyList<SyntaxToken> collation = collateKeyword is null ? [] : ParseQualifiedName();
        SyntaxToken? notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null;
        SyntaxToken? nullKeyword = notKeyword is null ? null : Expect(SyntaxKind.NullKeyword);
        SyntaxToken? assignment = IsPlpgsqlDefaultStart() ? Advance() : null;
        var defaultValue = assignment is null ? null : ParseExpression();
        return new PlpgsqlVariableDeclaration
        {
            Span = SourceSpan.From(name, PgPreviousToken()),
            Name = name,
            ConstantKeyword = constantKeyword,
            Type = type,
            TypeAttribute = typeAttribute,
            CollateKeyword = collateKeyword,
            Collation = collation,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
            AssignmentToken = assignment,
            Default = defaultValue,
        };
    }

    private bool IsPlpgsqlDefaultStart() =>
        _current.Kind is SyntaxKind.ColonEqualsToken or SyntaxKind.EqualsToken || IdentifierEquals(Keyword.Default);

    // EXCEPTION WHEN condition [OR condition ...] THEN statements ...
    private List<PlpgsqlExceptionHandler> ParsePlpgsqlExceptionHandlers()
    {
        var handlers = new List<PlpgsqlExceptionHandler>();
        do
        {
            var whenKeyword = Expect(SyntaxKind.WhenKeyword);
            var conditions = new List<PlpgsqlCondition> { ParsePlpgsqlCondition() };
            while (_current.Kind == SyntaxKind.OrKeyword)
            {
                Advance();
                conditions.Add(ParsePlpgsqlCondition());
            }

            var thenKeyword = Expect(SyntaxKind.ThenKeyword);
            var statements = ParsePlpgsqlStatements();
            handlers.Add(new PlpgsqlExceptionHandler
            {
                Span = SourceSpan.From(whenKeyword, PgPreviousToken()),
                WhenKeyword = whenKeyword,
                Conditions = conditions,
                ThenKeyword = thenKeyword,
                Statements = statements,
            });
        }
        while (_current.Kind == SyntaxKind.WhenKeyword);

        return handlers;
    }

    // SQLSTATE '22012', or a condition name such as division_by_zero or OTHERS.
    private PlpgsqlCondition ParsePlpgsqlCondition()
    {
        if (IdentifierEquals(Keyword.SqlState) && NextKind == SyntaxKind.String)
        {
            var sqlState = Advance();
            var state = Advance();
            return new PlpgsqlCondition { Span = SourceSpan.From(sqlState, state), SqlStateKeyword = sqlState, Name = state };
        }

        var name = Expect(SyntaxKind.Identifier);
        return new PlpgsqlCondition { Span = name.Span, Name = name };
    }

    // Statements, each ended by a semicolon, up to the keyword that closes the enclosing construct.
    private List<Query> ParsePlpgsqlStatements()
    {
        var statements = new List<Query>();
        while (!IsPlpgsqlListEnd())
        {
            statements.Add(ParsePlpgsqlStatement());
            Expect(SyntaxKind.Semicolon);
        }

        return statements;
    }

    private bool IsPlpgsqlListEnd() =>
        _current.Kind is SyntaxKind.EndKeyword or SyntaxKind.ElseKeyword or SyntaxKind.WhenKeyword or SyntaxKind.EndOfFile
        || IdentifierEquals(Keyword.PlpgsqlElsif) || IdentifierEquals(Keyword.ElseIf) || IdentifierEquals(Keyword.Exception);
}
