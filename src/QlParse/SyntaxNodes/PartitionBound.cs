namespace QlParse;

public sealed class PartitionBound
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? ValuesKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Values { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? ToOpen { get; init; }
    public IReadOnlyList<Expression> ToValues { get; init; } = [];
    public SyntaxToken? ToClose { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public StorageParameterList? HashParameters { get; init; }
}
