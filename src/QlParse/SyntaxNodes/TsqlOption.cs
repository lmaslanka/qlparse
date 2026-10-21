namespace QlParse;

// A module, atomic-block or durability option: words, then optionally `= value` (ENCRYPTION;
// RETURNS NULL ON NULL INPUT; EXECUTE AS OWNER; INLINE = ON; TRANSACTION ISOLATION LEVEL = SNAPSHOT).
// EXECUTE AS keeps its principal in Value without an EqualsToken.
public sealed class TsqlOption
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public IReadOnlyList<SyntaxToken> Value { get; init; } = [];
}
