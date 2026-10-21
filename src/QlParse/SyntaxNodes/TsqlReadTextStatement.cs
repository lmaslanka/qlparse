namespace QlParse;

// `READTEXT table.column text_pointer offset size [HOLDLOCK]`.
public sealed class TsqlReadTextStatement : Query
{
    public required SyntaxToken ReadTextKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Column { get; init; }
    public required Expression TextPointer { get; init; }
    public required Expression Offset { get; init; }
    public required Expression Size { get; init; }
    public SyntaxToken? HoldLockKeyword { get; init; }
}
