namespace QlParse;

public sealed class MethodSpecification
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OverridingKeyword { get; init; }
    public SyntaxToken? Kind { get; init; }
    public required SyntaxToken MethodKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Parameters { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken ReturnsKeyword { get; init; }
    public required DataType ReturnsType { get; init; }
    public SyntaxToken? ReturnsAsKeyword { get; init; }
    public SyntaxToken? ReturnsLocator { get; init; }
    public SyntaxToken? SpecificKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? SpecificName { get; init; }
    public SyntaxToken? SelfResultKeyword { get; init; }
    public SyntaxToken? SelfResultAsKeyword { get; init; }
    public SyntaxToken? ResultKeyword { get; init; }
    public SyntaxToken? SelfLocatorKeyword { get; init; }
    public SyntaxToken? SelfLocatorAsKeyword { get; init; }
    public SyntaxToken? LocatorKeyword { get; init; }
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
}
