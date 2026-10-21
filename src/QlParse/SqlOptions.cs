namespace QlParse;

[Flags]
public enum SqlOptions
{
    None = 0,
    AtParameters = 1,
    Postgres = 2,
    SqlServer = 4,

    // SQL Server's SET QUOTED_IDENTIFIER OFF: with SqlServer, "..." is a string literal instead of a delimited
    // identifier. It has no effect without SqlServer.
    QuotedIdentifierOff = 8,
}
