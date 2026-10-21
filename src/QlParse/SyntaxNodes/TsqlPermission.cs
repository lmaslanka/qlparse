namespace QlParse;

// One permission of GRANT / DENY / REVOKE, possibly several words (`ALTER ANY USER`, `VIEW DEFINITION`, `ALL PRIVILEGES`),
// with an optional column list.
public sealed class TsqlPermission
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
