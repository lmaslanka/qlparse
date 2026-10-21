namespace QlParse;

// A backup device: `{DISK | TAPE | URL} = {'path' | @variable}` or a logical device name or variable.
public sealed class TsqlBackupDevice
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? DeviceType { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public required SyntaxToken Name { get; init; }
}
