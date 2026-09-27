namespace QlParse;

public sealed class CreateTranslationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TranslationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> SourceCharacterSet { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TargetCharacterSet { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Existing { get; init; }
    public RoutineDesignator? Routine { get; init; }
}
