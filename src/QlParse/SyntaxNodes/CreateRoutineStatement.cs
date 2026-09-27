namespace QlParse;

public sealed class CreateRoutineStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? KindPrefix { get; init; }
    public required SyntaxToken RoutineKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Parameters { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? ReturnsKeyword { get; init; }
    public DataType? ReturnsType { get; init; }
    public SyntaxToken? ReturnsAsKeyword { get; init; }
    public SyntaxToken? ReturnsLocator { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ForType { get; init; }
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
    public SyntaxToken? SqlKeyword { get; init; }
    public Query? Body { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public SyntaxToken? NameKeyword { get; init; }
    public SyntaxToken? ExternalName { get; init; }
}
