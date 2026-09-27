namespace QlParse;

public sealed class AlterViewColumnAction : AlterViewAction
{
    public required AlterColumnAction Column { get; init; }
}
