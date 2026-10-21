namespace QlParse;

// Postgres trigger grammar: CREATE EVENT TRIGGER, the EXECUTE {FUNCTION | PROCEDURE} trigger action,
// and the CREATE / DROP TRIGGER clauses ISO lacks (OR REPLACE, CONSTRAINT, OR-ed events, TRUNCATE,
// FROM referenced table, deferrability, ON table on DROP), all reached under the Postgres flag only.
internal sealed partial class Parser
{
    internal readonly record struct PgTriggerPrefix(SyntaxToken? OrKeyword, SyntaxToken? ReplaceKeyword, SyntaxToken? ConstraintKeyword);

    internal readonly record struct PgTriggerConstraint(
        SyntaxToken? FromKeyword,
        IReadOnlyList<SyntaxToken>? ReferencedTable,
        SyntaxToken? NotKeyword,
        SyntaxToken? DeferrableKeyword,
        SyntaxToken? InitiallyKeyword,
        SyntaxToken? InitiallyWhen);

    internal readonly record struct PgDropTriggerTail(SyntaxToken? OnKeyword, IReadOnlyList<SyntaxToken>? TableName, SyntaxToken? Behavior);

    // [OR REPLACE] [CONSTRAINT] before TRIGGER.
    internal PgTriggerPrefix ParsePgTriggerPrefix()
    {
        SyntaxToken? orKeyword = null;
        SyntaxToken? replaceKeyword = null;
        if (_current.Kind == SyntaxKind.OrKeyword)
        {
            orKeyword = Advance();
            replaceKeyword = ExpectIdent(Keyword.PgReplace);
        }

        SyntaxToken? constraintKeyword = IdentifierEquals(Keyword.Constraint) ? Advance() : null;
        return new PgTriggerPrefix(orKeyword, replaceKeyword, constraintKeyword);
    }

    // OR event [OF columns] ...
    internal IReadOnlyList<PgTriggerEvent> ParsePgTriggerEvents()
    {
        var events = new List<PgTriggerEvent>();
        while (_current.Kind == SyntaxKind.OrKeyword)
        {
            var orKeyword = Advance();
            var eventKeyword = ParseTriggerEvent();
            SyntaxToken? ofKeyword = null;
            IReadOnlyList<SyntaxToken>? columns = null;
            if (eventKeyword.Kind == SyntaxKind.UpdateKeyword && _current.Kind == SyntaxKind.OfKeyword)
            {
                ofKeyword = Advance();
                columns = ParseNameList();
            }

            events.Add(new PgTriggerEvent
            {
                Span = SourceSpan.From(orKeyword, columns?[^1] ?? eventKeyword),
                OrKeyword = orKeyword,
                Event = eventKeyword,
                OfKeyword = ofKeyword,
                Columns = columns,
            });
        }

        return events;
    }

    // [FROM referenced_table] [NOT DEFERRABLE | [DEFERRABLE] [INITIALLY {IMMEDIATE | DEFERRED}]]
    internal PgTriggerConstraint ParsePgTriggerConstraint()
    {
        SyntaxToken? fromKeyword = null;
        IReadOnlyList<SyntaxToken>? referencedTable = null;
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
            referencedTable = ParseQualifiedName();
        }

        ParseConstraintCharacteristics(out var notKeyword, out var deferrable, out var initially, out var initiallyWhen);
        return new PgTriggerConstraint(fromKeyword, referencedTable, notKeyword, deferrable, initially, initiallyWhen);
    }

    internal bool IsPgExecuteFunctionStart() =>
        IdentifierEquals(Keyword.Execute) && (NextEquals(Keyword.Function) || NextEquals(Keyword.Procedure));

    // EXECUTE {FUNCTION | PROCEDURE} name ([argument, ...])
    internal PgExecuteFunctionClause ParsePgExecuteFunctionClause()
    {
        var executeKeyword = Advance();
        var functionKeyword = Advance();
        var name = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        IReadOnlyList<Expression> arguments = _current.Kind == SyntaxKind.CloseParen ? [] : ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgExecuteFunctionClause
        {
            Span = SourceSpan.From(executeKeyword, closeParen),
            ExecuteKeyword = executeKeyword,
            FunctionKeyword = functionKeyword,
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    // DROP TRIGGER [IF EXISTS] name ON table [CASCADE | RESTRICT]: the part after the name.
    internal PgDropTriggerTail ParsePgDropTriggerTail()
    {
        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return default;
        }

        var onKeyword = Advance();
        var tableName = ParseQualifiedName();
        return new PgDropTriggerTail(onKeyword, tableName, ParsePgOptionalBehavior());
    }

    // CREATE EVENT TRIGGER name ON event [WHEN variable IN ('v', ...) [AND ...]] EXECUTE {FUNCTION | PROCEDURE} f()
    private PgCreateEventTriggerStatement ParsePgCreateEventTrigger()
    {
        var createKeyword = Advance();
        var eventKeyword = Advance();
        var triggerKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var eventName = Expect(SyntaxKind.Identifier);
        SyntaxToken? whenKeyword = null;
        var filters = new List<PgEventTriggerFilter>();
        if (_current.Kind == SyntaxKind.WhenKeyword)
        {
            whenKeyword = Advance();
            filters.Add(ParsePgEventTriggerFilter(null));
            while (_current.Kind == SyntaxKind.AndKeyword)
            {
                filters.Add(ParsePgEventTriggerFilter(Advance()));
            }
        }

        if (!IsPgExecuteFunctionStart())
        {
            throw new SqlParseException($"Expected EXECUTE FUNCTION, found {_current.Kind}", _current.Position);
        }

        var execute = ParsePgExecuteFunctionClause();
        return new PgCreateEventTriggerStatement
        {
            Span = SourceSpan.From(createKeyword, execute.Span),
            CreateKeyword = createKeyword,
            EventKeyword = eventKeyword,
            TriggerKeyword = triggerKeyword,
            Name = name,
            OnKeyword = onKeyword,
            Event = eventName,
            WhenKeyword = whenKeyword,
            Filters = filters,
            Execute = execute,
        };
    }

    private PgEventTriggerFilter ParsePgEventTriggerFilter(SyntaxToken? andKeyword)
    {
        var variable = Expect(SyntaxKind.Identifier);
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        var values = ParsePgStringList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgEventTriggerFilter
        {
            Span = SourceSpan.From(andKeyword ?? variable, closeParen),
            AndKeyword = andKeyword,
            Variable = variable,
            InKeyword = inKeyword,
            OpenParen = openParen,
            Values = values,
            CloseParen = closeParen,
        };
    }
}
