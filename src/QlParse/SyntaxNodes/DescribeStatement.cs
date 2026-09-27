namespace QlParse;

public sealed class DescribeStatement : Query
{
    public required SyntaxToken DescribeKeyword { get; init; }
    public SyntaxToken? InputOrOutput { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken UsingOrInto { get; init; }
    public SyntaxToken? SqlKeyword { get; init; }
    public required SyntaxToken DescriptorKeyword { get; init; }
    public SyntaxToken? DescriptorScope { get; init; }
    public required SyntaxToken Descriptor { get; init; }
}
