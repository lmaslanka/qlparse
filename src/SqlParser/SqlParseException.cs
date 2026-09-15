namespace SqlParser;

public sealed class SqlParseException : Exception
{
    public SqlParseException(string message, int position)
        : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}
