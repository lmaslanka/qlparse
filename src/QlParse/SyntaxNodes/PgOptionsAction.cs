namespace QlParse;

public sealed class PgOptionsAction : PgAlterAction
{
    public required PgOptionList Options { get; init; }
}
