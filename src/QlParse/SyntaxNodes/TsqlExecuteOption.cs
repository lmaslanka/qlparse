namespace QlParse;

// RECOMPILE, or RESULT SETS {UNDEFINED | NONE | (definition, ...)}.
public sealed class TsqlExecuteOption
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlResultSetDefinition> ResultSets { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
