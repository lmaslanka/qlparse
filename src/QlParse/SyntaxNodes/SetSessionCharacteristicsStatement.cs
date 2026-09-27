namespace QlParse;

public sealed class SetSessionCharacteristicsStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SessionKeyword { get; init; }
    public required SyntaxToken CharacteristicsKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required IReadOnlyList<TransactionMode> Modes { get; init; }
}
