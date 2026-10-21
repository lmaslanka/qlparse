namespace QlParse;

// EXTERNAL NAME assembly.class[.method] of a CLR module.
public sealed class TsqlExternalName
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ExternalKeyword { get; init; }
    public required SyntaxToken NameKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}
