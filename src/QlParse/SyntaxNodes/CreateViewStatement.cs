namespace QlParse;

public sealed class CreateViewStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? RecursiveKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TypeName { get; init; }
    public SyntaxToken? UnderKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Superview { get; init; }
    public SyntaxToken? ColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnsClose { get; init; }
    public IReadOnlyList<TableElement>? Elements { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Query Query { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? Levels { get; init; }
    public SyntaxToken? CheckKeyword { get; init; }
    public SyntaxToken? OptionKeyword { get; init; }
}
