using System.Text;

namespace SqlParser;

public sealed class SqlStats
{
    public const string LexerStage = "lexer";
    public const string ParserStage = "parser";

    private const double MillisecondThreshold = 1;
    private const string MillisecondUnit = "ms";
    private const string MicrosecondUnit = "µs";

    public required int SourceChars { get; init; }
    public required int SourceLines { get; init; }
    public required int TokenCount { get; init; }
    public required int Comments { get; init; }
    public int? OutputChars { get; init; }
    public required TimeSpan Lexer { get; init; }
    public TimeSpan? Parser { get; init; }
    public TimeSpan? Formatter { get; init; }
    public TimeSpan? Dump { get; init; }
    public required TimeSpan Total { get; init; }
    public string? FailedStage { get; init; }

    public string Render()
    {
        var text = new StringBuilder();
        text.Append("Source: ");
        text.Append(SourceChars);
        text.Append(" chars, ");
        text.Append(SourceLines);
        text.Append(SourceLines == 1 ? " line" : " lines");
        text.AppendLine();
        text.Append("Tokens: ");
        text.Append(TokenCount);
        text.AppendLine();
        text.Append("Comments: ");
        text.Append(Comments);
        if (OutputChars is { } output)
        {
            text.AppendLine();
            text.Append("Output: ");
            text.Append(output);
            text.Append(" chars");
        }

        text.AppendLine();
        text.Append("Lexer: ");
        text.Append(FormatDuration(Lexer));
        if (Parser is { } parser)
        {
            text.AppendLine();
            text.Append("Parser: ");
            text.Append(FormatDuration(parser));
        }

        if (Formatter is { } formatter)
        {
            text.AppendLine();
            text.Append("Formatter: ");
            text.Append(FormatDuration(formatter));
        }

        if (Dump is { } dump)
        {
            text.AppendLine();
            text.Append("Dump: ");
            text.Append(FormatDuration(dump));
        }

        text.AppendLine();
        text.Append("Total: ");
        text.Append(FormatDuration(Total));
        if (FailedStage is not null)
        {
            text.AppendLine();
            text.Append("Failed: ");
            text.Append(FailedStage);
        }

        return text.ToString();
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds >= MillisecondThreshold)
        {
            return $"{duration.TotalMilliseconds:0.##} {MillisecondUnit}";
        }

        return $"{Math.Round(duration.TotalMicroseconds)} {MicrosecondUnit}";
    }
}
