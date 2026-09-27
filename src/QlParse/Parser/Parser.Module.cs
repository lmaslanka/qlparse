namespace QlParse;

internal sealed partial class Parser
{
    internal bool IsDeclareSection() =>
        (IdentifierEquals(Keyword.Begin) || _current.Kind == SyntaxKind.EndKeyword)
        && NextEquals(Keyword.Declare)
        && IsKeywordAt(_index + 1, Keyword.Section);

    private readonly record struct ModuleNamesClause(
        SyntaxToken? NamesKeyword,
        SyntaxToken? AreKeyword,
        IReadOnlyList<SyntaxToken>? CharacterSet);

    private readonly record struct ModuleSchemaClause(
        SyntaxToken? SchemaKeyword,
        IReadOnlyList<SyntaxToken>? SchemaName,
        SyntaxToken? AuthorizationKeyword,
        SyntaxToken? Authorization);

    private readonly record struct ModulePathClause(
        SyntaxToken? PathKeyword,
        List<IReadOnlyList<SyntaxToken>> Path);

    internal ModuleDefinition ParseModule()
    {
        var moduleKeyword = Advance();
        SyntaxToken? name = _current.Kind == SyntaxKind.Identifier && !IdentifierEquals(Keyword.Names)
            && !IdentifierEquals(Keyword.Language)
            ? Advance()
            : null;
        var namesClause = ParseModuleNamesClause();
        var languageKeyword = ExpectIdent(Keyword.Language);
        var language = Expect(SyntaxKind.Identifier);
        var schemaClause = ParseModuleSchemaClause();
        var pathClause = ParseModulePathClause();
        var contents = ParseModuleContents();
        var end = contents.Count > 0
            ? contents[^1].Span
            : schemaClause.Authorization?.Span ?? schemaClause.SchemaName?[^1].Span ?? language.Span;
        return new ModuleDefinition
        {
            Span = SourceSpan.From(moduleKeyword, end),
            ModuleKeyword = moduleKeyword,
            Name = name,
            NamesKeyword = namesClause.NamesKeyword,
            AreKeyword = namesClause.AreKeyword,
            CharacterSet = namesClause.CharacterSet,
            LanguageKeyword = languageKeyword,
            Language = language,
            SchemaKeyword = schemaClause.SchemaKeyword,
            SchemaName = schemaClause.SchemaName,
            AuthorizationKeyword = schemaClause.AuthorizationKeyword,
            Authorization = schemaClause.Authorization,
            PathKeyword = pathClause.PathKeyword,
            Path = pathClause.Path,
            Contents = contents,
        };
    }

    private ModuleNamesClause ParseModuleNamesClause()
    {
        if (!IdentifierEquals(Keyword.Names))
        {
            return default;
        }

        var namesKeyword = Advance();
        var areKeyword = ExpectIdent(Keyword.Are);
        var characterSet = ParseQualifiedName();
        return new ModuleNamesClause(namesKeyword, areKeyword, characterSet);
    }

    private ModuleSchemaClause ParseModuleSchemaClause()
    {
        SyntaxToken? schemaKeyword = null;
        IReadOnlyList<SyntaxToken>? schemaName = null;
        if (IdentifierEquals(Keyword.Schema))
        {
            schemaKeyword = Advance();
            schemaName = ParseQualifiedName();
        }

        SyntaxToken? authorizationKeyword = null;
        SyntaxToken? authorization = null;
        if (IdentifierEquals(Keyword.Authorization))
        {
            authorizationKeyword = Advance();
            authorization = Expect(SyntaxKind.Identifier);
        }

        if (schemaKeyword is null && authorizationKeyword is null)
        {
            throw new SqlParseException($"Expected SCHEMA or AUTHORIZATION, found {_current.Kind}", _current.Position);
        }

        return new ModuleSchemaClause(schemaKeyword, schemaName, authorizationKeyword, authorization);
    }

    private ModulePathClause ParseModulePathClause()
    {
        if (!IdentifierEquals(Keyword.Path))
        {
            return new ModulePathClause(null, []);
        }

        var pathKeyword = Advance();
        var path = new List<IReadOnlyList<SyntaxToken>> { ParseQualifiedName() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            path.Add(ParseQualifiedName());
        }

        return new ModulePathClause(pathKeyword, path);
    }

    private List<Query> ParseModuleContents()
    {
        var contents = new List<Query>();
        while (IdentifierEquals(Keyword.Procedure) || IsDeclareCursor() || _current.Kind == SyntaxKind.Semicolon)
        {
            if (_current.Kind == SyntaxKind.Semicolon)
            {
                Advance();
                continue;
            }

            contents.Add(IdentifierEquals(Keyword.Procedure) ? ParseModuleProcedure() : ParseStatement());
            if (_current.Kind is SyntaxKind.Semicolon)
            {
                Advance();
            }
        }

        return contents;
    }

    private ModuleProcedure ParseModuleProcedure()
    {
        var procedureKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? openParen = null;
        IReadOnlyList<RoutineParameter> parameters = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            ParseParameterList(out var open, out parameters, out var close);
            openParen = open;
            closeParen = close;
        }

        var semicolon = _current.Kind == SyntaxKind.Semicolon ? Advance() : (SyntaxToken?)null;
        var statement = ParseStatement();
        var statementSemicolon = _current.Kind == SyntaxKind.Semicolon ? Advance() : (SyntaxToken?)null;
        var end = statementSemicolon?.Span ?? statement.Span;
        return new ModuleProcedure
        {
            Span = SourceSpan.From(procedureKeyword, end),
            ProcedureKeyword = procedureKeyword,
            Name = name,
            OpenParen = openParen,
            Parameters = parameters,
            CloseParen = closeParen,
            Semicolon = semicolon,
            Statement = statement,
            StatementSemicolon = statementSemicolon,
        };
    }

    internal EmbeddedSqlStatement ParseEmbeddedSql()
    {
        var execKeyword = Advance();
        var sqlKeyword = ExpectIdent(Keyword.Sql);
        var statement = ParseStatement();
        SyntaxToken? endKeyword = null;
        SyntaxToken? minus = null;
        SyntaxToken? endExec = null;
        SyntaxToken? semicolon = null;
        if (_current.Kind == SyntaxKind.Semicolon)
        {
            semicolon = Advance();
        }
        else
        {
            if (_current.Kind == SyntaxKind.EndKeyword
            && NextKind == SyntaxKind.MinusToken
            && IsKeywordAt(_index + 1, Keyword.Exec))
            {
                endKeyword = Advance();
                minus = Advance();
                endExec = Advance();
            }
        }

        var end = semicolon?.Span ?? endExec?.Span ?? statement.Span;
        return new EmbeddedSqlStatement
        {
            Span = SourceSpan.From(execKeyword, end),
            ExecKeyword = execKeyword,
            SqlKeyword = sqlKeyword,
            Statement = statement,
            EndKeyword = endKeyword,
            Minus = minus,
            EndExec = endExec,
            Semicolon = semicolon,
        };
    }

    internal DeclareSectionStatement ParseDeclareSection()
    {
        var beginOrEnd = Advance();
        var declareKeyword = ExpectIdent(Keyword.Declare);
        var sectionKeyword = ExpectIdent(Keyword.Section);
        return new DeclareSectionStatement
        {
            Span = SourceSpan.From(beginOrEnd, sectionKeyword),
            BeginOrEnd = beginOrEnd,
            DeclareKeyword = declareKeyword,
            SectionKeyword = sectionKeyword,
        };
    }

    internal WheneverStatement ParseWhenever()
    {
        var wheneverKeyword = Advance();
        SyntaxToken? notKeyword = null;
        SyntaxToken condition;
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            notKeyword = Advance();
            condition = ExpectIdent(Keyword.Found);
        }
        else
        {
            if (IdentifierEquals(Keyword.SqlError) || IdentifierEquals(Keyword.Sqlwarning))
            {
                condition = Advance();
            }
            else
            {
                throw new SqlParseException($"Expected SQLERROR, SQLWARNING, or NOT FOUND, found {_current.Kind}", _current.Position);
            }
        }

        SyntaxToken action;
        SyntaxToken? goKeyword = null;
        SyntaxToken? toKeyword = null;
        SyntaxToken? target = null;
        if (IdentifierEquals(Keyword.Continue))
        {
            action = Advance();
        }
        else
        {
            if (IdentifierEquals(Keyword.Goto))
            {
                action = Advance();
                target = Expect(SyntaxKind.Identifier);
            }
            else
            {
                if (IdentifierEquals(Keyword.Go))
                {
                    goKeyword = Advance();
                    action = goKeyword.Value;
                    toKeyword = ExpectIdent(Keyword.To);
                    target = Expect(SyntaxKind.Identifier);
                }
                else
                {
                    throw new SqlParseException($"Expected CONTINUE, GOTO, or GO TO, found {_current.Kind}", _current.Position);
                }
            }
        }

        var end = target?.Span ?? action.Span;
        return new WheneverStatement
        {
            Span = SourceSpan.From(wheneverKeyword, end),
            WheneverKeyword = wheneverKeyword,
            NotKeyword = notKeyword,
            Condition = condition,
            Action = action,
            GoKeyword = goKeyword,
            ToKeyword = toKeyword,
            Target = target,
        };
    }
}
