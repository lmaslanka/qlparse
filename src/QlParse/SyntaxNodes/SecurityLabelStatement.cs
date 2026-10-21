namespace QlParse;

public sealed class SecurityLabelStatement : Query
{
    public required SyntaxToken SecurityKeyword { get; init; }
    public required SyntaxToken LabelKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? Provider { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ObjectKind { get; init; }
    public required ObjectSignature Object { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}
