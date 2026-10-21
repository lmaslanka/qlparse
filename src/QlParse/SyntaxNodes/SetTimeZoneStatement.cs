namespace QlParse;

public sealed class SetTimeZoneStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public required SyntaxToken TimeKeyword { get; init; }
    public required SyntaxToken ZoneKeyword { get; init; }
    public SyntaxToken? LocalKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public Expression? Value { get; init; }
}
