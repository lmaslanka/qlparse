namespace QlParse;

// `expression AT TIME ZONE zone`: converts a datetime value to a datetimeoffset in the named zone.
public sealed class TsqlAtTimeZoneExpression : Expression
{
    public required Expression Expression { get; init; }
    public required SyntaxToken AtKeyword { get; init; }
    public required SyntaxToken TimeKeyword { get; init; }
    public required SyntaxToken ZoneKeyword { get; init; }
    public required Expression TimeZone { get; init; }
}
