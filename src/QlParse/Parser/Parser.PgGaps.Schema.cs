namespace QlParse;

// Postgres schema-object gaps: ALTER FOREIGN TABLE column/constraint actions, foreign table CHECK
// constraints, ALTER EXTENSION TRANSFORM / MATERIALIZED VIEW members, and the Postgres ALTER DOMAIN actions.
internal sealed partial class Parser
{
    internal PgAlterContext PgForeignTableContextOf(List<SyntaxToken> objectKind) =>
        objectKind.Count == Len2 && TokenEquals(objectKind[0], Keyword.Foreign) && TokenEquals(objectKind[1], Keyword.Table)
            ? PgAlterContext.ForeignTable
            : PgAlterContext.General;

    // ALTER FOREIGN TABLE ... {ADD | DROP | ALTER} ...: the ALTER TABLE action grammar.
    internal PgForeignTableAction ParsePgForeignTableAction()
    {
        var action = ParseAlterTableAction();
        return new PgForeignTableAction { Span = action.Span, Action = action };
    }

    // ALTER [COLUMN] c OPTIONS ([ADD | SET | DROP] option ['value'] [, ...])
    internal AlterColumnAction? TryParsePgAlterColumnOptions(AlterColumnHead head)
    {
        if (!IsPgOptionsStart())
        {
            return null;
        }

        var options = ParsePgOptionList();
        return new AlterColumnAction
        {
            Span = SourceSpan.From(head.AlterKeyword, options.Span),
            AlterKeyword = head.AlterKeyword,
            ColumnKeyword = head.ColumnKeyword,
            Name = head.Name,
            Options = options,
        };
    }

    internal readonly record struct PgForeignElements(List<PgForeignColumn> Columns, IReadOnlyList<TableConstraint>? Constraints);

    // The column list of CREATE FOREIGN TABLE: columns and table-level CHECK constraints, comma-separated.
    internal PgForeignElements ParsePgForeignElements()
    {
        var columns = new List<PgForeignColumn>();
        List<TableConstraint>? constraints = null;
        if (_current.Kind == SyntaxKind.CloseParen)
        {
            return new PgForeignElements(columns, null);
        }

        while (true)
        {
            if (IsTableConstraintStart())
            {
                (constraints ??= []).Add(ParseTableConstraint());
            }
            else
            {
                columns.Add(ParsePgForeignColumn());
            }

            if (_current.Kind != SyntaxKind.Comma)
            {
                return new PgForeignElements(columns, constraints);
            }

            Advance();
        }
    }

    // ALTER EXTENSION {ADD | DROP} members that the generic ALTER / DROP statements do not address.
    internal PgObjectKind MatchPgExtensionOnlyMemberKind(int index)
    {
        if (IsKeywordAt(index, Keyword.DdlMaterialized) && IsKeywordAt(index + 1, Keyword.View))
        {
            return PgObjectKind.Plain(Len2);
        }

        return IsKeywordAt(index, Keyword.Transform) ? new PgObjectKind(1, PgObjectShape.Transform, false, false) : default;
    }

    // TRANSFORM FOR type LANGUAGE lang
    internal PgObjectReference ParsePgTransformReference()
    {
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var type = ParseDataType();
        var languageKeyword = ExpectIdent(Keyword.Language);
        var language = Expect(SyntaxKind.Identifier);
        return new PgObjectReference
        {
            Span = SourceSpan.From(forKeyword, language),
            ForKeyword = forKeyword,
            Arguments = [new PgRoutineArgument { Span = type.Span, Type = type }],
            LanguageKeyword = languageKeyword,
            Language = language,
        };
    }

    // ALTER DOMAIN d SET NOT NULL | SET SCHEMA s
    internal PgDomainAction ParsePgDomainSet()
    {
        var setKeyword = Advance();
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            var notKeyword = Advance();
            var nullKeyword = Expect(SyntaxKind.NullKeyword);
            return new PgDomainAction { Span = SourceSpan.From(setKeyword, nullKeyword), Keywords = [setKeyword, notKeyword, nullKeyword] };
        }

        var schemaKeyword = ExpectIdent(Keyword.Schema);
        var schema = Expect(SyntaxKind.Identifier);
        return new PgDomainAction
        {
            Span = SourceSpan.From(setKeyword, schema),
            Keywords = [setKeyword, schemaKeyword],
            Target = schema,
        };
    }

    // ALTER DOMAIN d VALIDATE CONSTRAINT c | RENAME [CONSTRAINT a] TO b | OWNER TO r
    internal PgDomainAction ParsePgDomainAction()
    {
        if (IdentifierEquals(Keyword.DdlValidate))
        {
            var validateKeyword = Advance();
            var constraintKeyword = ExpectIdent(Keyword.Constraint);
            var name = Expect(SyntaxKind.Identifier);
            return new PgDomainAction
            {
                Span = SourceSpan.From(validateKeyword, name),
                Keywords = [validateKeyword, constraintKeyword],
                Target = name,
            };
        }

        if (!IdentifierEquals(Keyword.Rename) && !IdentifierEquals(Keyword.DdlOwner))
        {
            throw new SqlParseException($"Expected SET, ADD, DROP, RENAME, VALIDATE, or OWNER, found {_current.Kind}", _current.Position);
        }

        var keywords = new List<SyntaxToken> { Advance() };
        SyntaxToken? target = null;
        if (IdentifierEquals(Keyword.Constraint))
        {
            keywords.Add(Advance());
            target = Expect(SyntaxKind.Identifier);
        }

        var toKeyword = ExpectIdent(Keyword.To);
        var newName = ParseGrantee();
        return new PgDomainAction
        {
            Span = SourceSpan.From(keywords[0], newName),
            Keywords = keywords,
            Target = target,
            ToKeyword = toKeyword,
            NewName = newName,
        };
    }

    // ALTER DOMAIN d DROP NOT NULL | DROP CONSTRAINT [IF EXISTS] c [CASCADE | RESTRICT]
    internal AlterDomainAction ParsePgDropDomainConstraintOrNotNull(SyntaxToken dropKeyword)
    {
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            var notKeyword = Advance();
            var nullKeyword = Expect(SyntaxKind.NullKeyword);
            return new PgDomainAction { Span = SourceSpan.From(dropKeyword, nullKeyword), Keywords = [dropKeyword, notKeyword, nullKeyword] };
        }

        var constraintKeyword = ExpectIdent(Keyword.Constraint);
        var ifExists = TryParsePgExistenceCheck();
        var name = Expect(SyntaxKind.Identifier);
        var behavior = TryParseDropBehavior();
        return new DropDomainConstraintAction
        {
            Span = SourceSpan.From(dropKeyword, behavior ?? name),
            DropKeyword = dropKeyword,
            ConstraintKeyword = constraintKeyword,
            IfExists = ifExists,
            Name = name,
            Behavior = behavior,
        };
    }

    internal readonly record struct PgNotValid(SyntaxToken? NotKeyword, SyntaxToken? ValidKeyword);

    // NOT VALID after ALTER DOMAIN ... ADD CONSTRAINT.
    internal PgNotValid TryParsePgNotValid()
    {
        if (_current.Kind != SyntaxKind.NotKeyword || !NextEquals(Keyword.DdlValid))
        {
            return default;
        }

        var notKeyword = Advance();
        return new PgNotValid(notKeyword, Advance());
    }
}
