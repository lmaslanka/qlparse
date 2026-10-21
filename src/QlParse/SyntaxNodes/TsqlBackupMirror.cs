namespace QlParse;

// `MIRROR TO device [, ...]`.
public sealed class TsqlBackupMirror
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken MirrorKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<TsqlBackupDevice> Devices { get; init; }
}
