namespace QlParse;

public sealed class PrivilegeObject
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Kind { get; init; }
    public IReadOnlyList<SyntaxToken> ExtraKindKeywords { get; init; } = [];
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
    public RoutineDesignator? Routine { get; init; }
    public IReadOnlyList<ObjectSignature> Targets { get; init; } = [];
}
