namespace QlParse;

public sealed class ExecuteStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public SyntaxToken? IntoSqlKeyword { get; init; }
    public SyntaxToken? IntoDescriptorKeyword { get; init; }
    public SyntaxToken? IntoDescriptorScope { get; init; }
    public SyntaxToken? IntoDescriptor { get; init; }
    public IReadOnlyList<SyntaxToken>? Targets { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? UsingSqlKeyword { get; init; }
    public SyntaxToken? UsingDescriptorKeyword { get; init; }
    public SyntaxToken? UsingDescriptorScope { get; init; }
    public SyntaxToken? UsingDescriptor { get; init; }
    public IReadOnlyList<SyntaxToken>? Arguments { get; init; }
}
