namespace QlParse;

// `BACKUP {DATABASE | LOG} name [file_or_filegroup, ...] TO device, ... [MIRROR TO ...] [WITH option, ...]`.
public sealed class TsqlBackupStatement : Query
{
    public required SyntaxToken BackupKeyword { get; init; }
    public required SyntaxToken TargetKind { get; init; }
    public required SyntaxToken Database { get; init; }
    public TsqlOptionList? Files { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<TsqlBackupDevice> Devices { get; init; }
    public IReadOnlyList<TsqlBackupMirror> Mirrors { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
