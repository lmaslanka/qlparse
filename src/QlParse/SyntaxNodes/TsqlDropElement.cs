namespace QlParse;

// One item of a T-SQL ALTER TABLE ... DROP list (`PERIOD FOR SYSTEM_TIME` has KindKeyword PERIOD).
public sealed class TsqlDropElement
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
