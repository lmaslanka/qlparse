namespace QlParse;

// label: (a GOTO target; the statements after it are separate statements of the enclosing list).
public sealed class TsqlLabelStatement : Query
{
    public required SyntaxToken Label { get; init; }
    public required SyntaxToken Colon { get; init; }
}
