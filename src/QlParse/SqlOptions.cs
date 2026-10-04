namespace QlParse;

[Flags]
public enum SqlOptions
{
    None = 0,
    AtParameters = 1,
    Postgres = 2,
    SqlServer = 4,
}
