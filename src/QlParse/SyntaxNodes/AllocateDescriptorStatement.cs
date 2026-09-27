namespace QlParse;

public sealed class AllocateDescriptorStatement : Query
{
    public required SyntaxToken AllocateKeyword { get; init; }
    public SyntaxToken? SqlKeyword { get; init; }
    public required SyntaxToken DescriptorKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? MaxKeyword { get; init; }
    public SyntaxToken? Occurrences { get; init; }
}
