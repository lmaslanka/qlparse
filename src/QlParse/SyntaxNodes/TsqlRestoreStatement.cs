namespace QlParse;

// `RESTORE {DATABASE | LOG} name [files] [FROM device, ...] [WITH ...]` and the
// `RESTORE {HEADERONLY | FILELISTONLY | VERIFYONLY | LABELONLY | REWINDONLY} FROM device, ... [WITH ...]` forms.
public sealed class TsqlRestoreStatement : Query
{
    public required SyntaxToken RestoreKeyword { get; init; }
    public required SyntaxToken TargetKind { get; init; }
    public SyntaxToken? Database { get; init; }
    public TsqlOptionList? Files { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public IReadOnlyList<TsqlBackupDevice> Devices { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
