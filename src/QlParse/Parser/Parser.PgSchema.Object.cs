namespace QlParse;

// Object kinds addressed by the generic Postgres ALTER / DROP statements, and the reference shapes
// they take: a plain name, a routine signature, an operator signature, `name USING am`, or `(a AS b)`.
internal sealed partial class Parser
{
    internal enum PgObjectShape
    {
        None,
        Name,
        Routine,
        Operator,
        OperatorClass,
        Cast,
        Trigger,
        Transform,
    }

    internal readonly record struct PgObjectKind(int Count, PgObjectShape Shape, bool HasIsoDrop, bool HasIsoAlter)
    {
        internal static PgObjectKind Plain(int count) => new(count, PgObjectShape.Name, false, false);
    }

    private const int ThreeWords = 3;

    internal PgObjectKind MatchPgObjectKind(int index)
    {
        if (index >= _tokens.Count)
        {
            return default;
        }

        var token = _tokens[index];
        if (token.Kind == SyntaxKind.CastKeyword)
        {
            return new PgObjectKind(1, PgObjectShape.Cast, false, false);
        }

        if (token.Kind != SyntaxKind.Identifier)
        {
            return default;
        }

        var text = token.TextOf(_source);
        return text.Length <= Len7 ? MatchPgObjectKindShort(text, index) : MatchPgObjectKindLong(text, index);
    }

    private PgObjectKind MatchPgObjectKindShort(ReadOnlySpan<char> text, int index) => text.Length switch
    {
        Len4 when TextEquals(text, Keyword.Type) => new PgObjectKind(1, PgObjectShape.Name, true, false),
        Len4 when TextEquals(text, Keyword.PgText) && IsPgSearchAt(index + 1) => PgObjectKind.Plain(ThreeWords),
        Len5 when TextEquals(text, Keyword.PgEvent) && IsKeywordAt(index + 1, Keyword.Trigger) => PgObjectKind.Plain(Len2),
        Len6 when TextEquals(text, Keyword.PgServer) => PgObjectKind.Plain(1),
        Len6 when TextEquals(text, Keyword.Domain) => new PgObjectKind(1, PgObjectShape.Name, true, true),
        Len6 when TextEquals(text, Keyword.PgAccess) && IsKeywordAt(index + 1, Keyword.Method) => PgObjectKind.Plain(Len2),
        Len7 when TextEquals(text, Keyword.Foreign) => MatchPgForeignKind(index + 1),
        Len7 when TextEquals(text, Keyword.Trigger) => new PgObjectKind(1, PgObjectShape.Trigger, false, false),
        _ => default,
    };

    private PgObjectKind MatchPgObjectKindLong(ReadOnlySpan<char> text, int index) => text.Length switch
    {
        Len8 when TextEquals(text, Keyword.Language) => PgObjectKind.Plain(1),
        Len8 when TextEquals(text, Keyword.Operator) => MatchPgOperatorKind(index + 1),
        Len9 when TextEquals(text, Keyword.PgExtension) => PgObjectKind.Plain(1),
        Len9 when TextEquals(text, Keyword.PgAggregate) => new PgObjectKind(1, PgObjectShape.Routine, false, false),
        Len9 when TextEquals(text, Keyword.Collation) => new PgObjectKind(1, PgObjectShape.Name, true, false),
        Len10 when TextEquals(text, Keyword.PgProcedural) && IsKeywordAt(index + 1, Keyword.Language) => PgObjectKind.Plain(Len2),
        Len10 when TextEquals(text, Keyword.PgConversion) || TextEquals(text, Keyword.PgStatistics) => PgObjectKind.Plain(1),
        Len11 when TextEquals(text, Keyword.PgPublication) => PgObjectKind.Plain(1),
        Len12 when TextEquals(text, Keyword.PgSubscription) => PgObjectKind.Plain(1),
        _ => default,
    };

    private PgObjectKind MatchPgForeignKind(int index)
    {
        if (IsKeywordAt(index, Keyword.Data) && IsKeywordAt(index + 1, Keyword.Wrapper))
        {
            return PgObjectKind.Plain(ThreeWords);
        }

        return IsKeywordAt(index, Keyword.Table) ? PgObjectKind.Plain(Len2) : default;
    }

    private PgObjectKind MatchPgOperatorKind(int index) =>
        IsKeywordAt(index, Keyword.PgClass) || IsKeywordAt(index, Keyword.PgFamily)
            ? new PgObjectKind(Len2, PgObjectShape.OperatorClass, false, false)
            : new PgObjectKind(1, PgObjectShape.Operator, false, false);

    // ALTER EXTENSION ADD|DROP members also name routines and the ISO object kinds.
    internal PgObjectKind MatchPgMemberKind(int index)
    {
        var kind = MatchPgObjectKind(index);
        if (kind.Shape != PgObjectShape.None)
        {
            return kind.Shape == PgObjectShape.Trigger ? default : kind;
        }

        if (IsKeywordAt(index, Keyword.Function) || IsKeywordAt(index, Keyword.Procedure) || IsKeywordAt(index, Keyword.Routine))
        {
            return new PgObjectKind(1, PgObjectShape.Routine, false, false);
        }

        if (IsKeywordAt(index, Keyword.Schema) || IsKeywordAt(index, Keyword.Sequence)
            || IsKeywordAt(index, Keyword.Table) || IsKeywordAt(index, Keyword.View))
        {
            return PgObjectKind.Plain(1);
        }

        return MatchPgExtensionOnlyMemberKind(index);
    }

    internal PgObjectReference ParsePgObjectReference(PgObjectShape shape) => shape switch
    {
        PgObjectShape.Routine => ParsePgRoutineReference(),
        PgObjectShape.Operator => ParsePgOperatorReference(),
        PgObjectShape.OperatorClass => ParsePgOperatorClassReference(),
        PgObjectShape.Cast => ParsePgCastReference(),
        PgObjectShape.Transform => ParsePgTransformReference(),
        _ => ParsePgNameReference(),
    };

    private PgObjectReference ParsePgNameReference()
    {
        var name = ParseQualifiedName();
        return new PgObjectReference { Span = SourceSpan.From(name[0], name[^1]), Name = name };
    }

    // name [( [mode] type, ... [ORDER BY ...] | * )]
    internal PgObjectReference ParsePgRoutineReference()
    {
        var name = ParseQualifiedName();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new PgObjectReference { Span = SourceSpan.From(name[0], name[^1]), Name = name };
        }

        var openParen = Advance();
        SyntaxToken? star = _current.Kind == SyntaxKind.Star ? Advance() : null;
        IReadOnlyList<PgRoutineArgument> arguments = [];
        if (star is null && _current.Kind is not SyntaxKind.CloseParen and not SyntaxKind.OrderKeyword)
        {
            arguments = ParsePgRoutineArguments();
        }

        SyntaxToken? orderKeyword = null;
        SyntaxToken? byKeyword = null;
        IReadOnlyList<PgRoutineArgument> ordered = [];
        if (_current.Kind == SyntaxKind.OrderKeyword)
        {
            orderKeyword = Advance();
            byKeyword = Expect(SyntaxKind.ByKeyword);
            ordered = ParsePgRoutineArguments();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgObjectReference
        {
            Span = SourceSpan.From(name[0], closeParen),
            Name = name,
            OpenParen = openParen,
            Star = star,
            Arguments = arguments,
            OrderKeyword = orderKeyword,
            ByKeyword = byKeyword,
            OrderedArguments = ordered,
            CloseParen = closeParen,
        };
    }

    // op [(left, right)] — NONE stands for the missing operand of a prefix operator.
    private PgObjectReference ParsePgOperatorReference()
    {
        var operatorName = ParsePgOperatorName();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new PgObjectReference { Span = operatorName.Span, Operator = operatorName };
        }

        var openParen = Advance();
        var arguments = ParsePgRoutineArguments();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgObjectReference
        {
            Span = SourceSpan.From(operatorName.Span, closeParen),
            Operator = operatorName,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    private PgObjectReference ParsePgOperatorClassReference()
    {
        var name = ParseQualifiedName();
        var usingKeyword = Expect(SyntaxKind.UsingKeyword);
        var accessMethod = Expect(SyntaxKind.Identifier);
        return new PgObjectReference
        {
            Span = SourceSpan.From(name[0], accessMethod),
            Name = name,
            UsingKeyword = usingKeyword,
            AccessMethod = accessMethod,
        };
    }

    private PgObjectReference ParsePgCastReference()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var source = ParsePgRoutineArgument();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var target = ParsePgRoutineArgument();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgObjectReference
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Arguments = [source, target],
            AsKeyword = asKeyword,
            CloseParen = closeParen,
        };
    }

    // DROP <kind> [IF EXISTS] object [, ...] [CASCADE | RESTRICT]
    private PgDropStatement ParsePgDrop(PgObjectKind kind)
    {
        var dropKeyword = Advance();
        var objectKind = ParsePgObjectKindTokens(kind.Count);
        var ifExists = ParsePgIfExists();
        var objects = new List<PgObjectReference> { ParsePgObjectReference(kind.Shape) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            objects.Add(ParsePgObjectReference(kind.Shape));
        }

        var behavior = ParsePgOptionalBehavior();
        return new PgDropStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior?.Span ?? objects[^1].Span),
            DropKeyword = dropKeyword,
            ObjectKind = objectKind,
            IfKeyword = ifExists.IfKeyword,
            ExistsKeyword = ifExists.ExistsKeyword,
            Objects = objects,
            Behavior = behavior,
        };
    }

    // {CREATE | ALTER | DROP} USER MAPPING [IF [NOT] EXISTS] FOR role SERVER name [OPTIONS (...)]
    private PgUserMappingStatement ParsePgUserMapping()
    {
        var verb = Advance();
        var userKeyword = Advance();
        var mappingKeyword = Advance();
        var ifExists = TokenEquals(verb, Keyword.Create) ? ParsePgIfNotExists() : ParsePgIfExists();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var user = ExpectPgRole();
        var serverKeyword = ExpectIdent(Keyword.PgServer);
        var server = Expect(SyntaxKind.Identifier);
        var options = TokenEquals(verb, Keyword.Drop) ? null : ParsePgOptionalOptionList();
        return new PgUserMappingStatement
        {
            Span = SourceSpan.From(verb, options?.Span ?? server.Span),
            Verb = verb,
            UserKeyword = userKeyword,
            MappingKeyword = mappingKeyword,
            IfKeyword = ifExists.IfKeyword,
            NotKeyword = ifExists.NotKeyword,
            ExistsKeyword = ifExists.ExistsKeyword,
            ForKeyword = forKeyword,
            User = user,
            ServerKeyword = serverKeyword,
            Server = server,
            Options = options,
        };
    }
}
