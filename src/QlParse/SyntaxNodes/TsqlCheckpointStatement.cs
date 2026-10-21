namespace QlParse;

// `CHECKPOINT [checkpoint_duration]`.
public sealed class TsqlCheckpointStatement : Query
{
    public required SyntaxToken CheckpointKeyword { get; init; }
    public SyntaxToken? Duration { get; init; }
}
