namespace QlParse;

// `[ADD | DROP] [CONTRACT] name` in a service's contract list.
public sealed class TsqlServiceContract
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Action { get; init; }
    public SyntaxToken? ContractKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
