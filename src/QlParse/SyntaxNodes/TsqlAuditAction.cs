namespace QlParse;

// One `{ADD | DROP} (action_group)` or `{ADD | DROP} (action, ... ON [class ::] securable BY principal, ...)` of an audit specification.
public sealed class TsqlAuditAction
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ActionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlPermission> Actions { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public TsqlSecurable? Securable { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Principals { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
