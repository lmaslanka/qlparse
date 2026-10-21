namespace QlParse;

// WHILE condition statement.
public sealed class TsqlWhileStatement : Query
{
    public required SyntaxToken WhileKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required Query Body { get; init; }
}
