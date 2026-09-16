using System.Diagnostics;
using SqlParser;
using SqlParser.Cli;

string? path = null;
var showLexer = false;
var showParser = false;
var showFormat = false;
var showStats = false;
int? repeat = null;
bool? colorOverride = null;

foreach (var arg in args)
{
    switch (arg)
    {
        case Flag.Lexer:
            showLexer = true;
            break;
        case Flag.Parser:
            showParser = true;
            break;
        case Flag.Format:
            showFormat = true;
            break;
        case Flag.Stats:
            showStats = true;
            break;
        case Flag.Color:
            colorOverride = true;
            break;
        case Flag.NoColor:
            colorOverride = false;
            break;
        case Flag.HelpShort or Flag.Help:
            PrintUsage(Console.Out);
            return 0;
        default:
            if (arg.StartsWith(Flag.RepeatPrefix))
            {
                if (!int.TryParse(arg.AsSpan(Flag.RepeatPrefix.Length), out var count) || count < 1)
                {
                    Console.Error.WriteLine($"Invalid {Flag.RepeatPrefix} value.");
                    PrintUsage(Console.Error);
                    return 1;
                }

                repeat = count;
                break;
            }

            if (arg.StartsWith('-'))
            {
                Console.Error.WriteLine($"Unknown option: {arg}");
                PrintUsage(Console.Error);
                return 1;
            }

            if (path is not null)
            {
                Console.Error.WriteLine("Only one input file is allowed.");
                PrintUsage(Console.Error);
                return 1;
            }

            path = arg;
            break;
    }
}

if (path is null)
{
    PrintUsage(Console.Error);
    return 1;
}

if (!showLexer && !showParser && !showFormat && !showStats)
{
    showFormat = true;
}

if (Directory.Exists(path))
{
    if (showLexer || showParser || showStats)
    {
        Console.Error.WriteLine("dump flags require a file");
        return 1;
    }

    if (repeat is not null)
    {
        Console.Error.WriteLine($"{Flag.RepeatPrefix} requires a file");
        return 1;
    }

    return FolderFormat.Run(path, Console.Error);
}

if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

var sql = File.ReadAllText(path);
if (repeat is { } repeats)
{
    return RunRepeat(sql, repeats);
}

var color = ShouldColor(colorOverride);
var formatOnly = showFormat && !showLexer && !showParser && !showStats;
if (formatOnly)
{
    try
    {
        Console.WriteLine(Sql.Format(sql, color));
        return 0;
    }
    catch (SqlParseException ex)
    {
        Console.Error.WriteLine($"{ex.Message} at {ex.Position}");
        return 1;
    }
}

var needFormat = showFormat || (showStats && !showLexer && !showParser);
var trace = Sql.Trace(sql, showLexer, showParser, needFormat, showStats);
var printed = false;

if (showLexer)
{
    PrintStage(StageName.Lexer, trace.Lexer);
    printed = true;
}

if (showParser)
{
    if (printed)
    {
        Console.WriteLine();
    }

    PrintStage(StageName.Parser, trace.Parser ?? StageName.ParseFailed);
    printed = true;
}

if (showFormat)
{
    if (printed)
    {
        Console.WriteLine();
    }

    var formatted = trace.Format;
    if (formatted is not null && color)
    {
        formatted = Sql.Format(sql, color: true);
    }

    PrintStage(StageName.Format, formatted ?? StageName.ParseFailed);
    printed = true;
}

if (showStats)
{
    if (printed)
    {
        Console.WriteLine();
    }

    PrintStage(StageName.Stats, $"{StageName.FilePrefix}{path}{Environment.NewLine}{trace.Stats.Render()}");
}

if (trace.Error is not null)
{
    Console.Error.WriteLine($"{trace.Error.Message} at {trace.Error.Position}");
    return 1;
}

return 0;

static int RunRepeat(string sql, int repeats)
{
    try
    {
        Sql.Format(sql);
    }
    catch (SqlParseException ex)
    {
        Console.Error.WriteLine($"{ex.Message} at {ex.Position}");
        return 1;
    }

    var watch = Stopwatch.StartNew();
    for (var i = 0; i < repeats; i++)
    {
        Sql.Format(sql);
    }

    watch.Stop();
    Console.WriteLine($"Repeats: {repeats}");
    Console.WriteLine($"Total: {SqlStats.FormatDuration(watch.Elapsed)}");
    Console.WriteLine($"Mean: {SqlStats.FormatDuration(watch.Elapsed / repeats)}");
    return 0;
}

static void PrintStage(string name, string body)
{
    Console.WriteLine($"== {name} ==");
    Console.WriteLine(body);
}

static bool ShouldColor(bool? colorOverride)
{
    if (colorOverride is { } value)
    {
        return value;
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Flag.NoColorVariable)))
    {
        return false;
    }

    return !Console.IsOutputRedirected;
}

static void PrintUsage(TextWriter output)
{
    output.WriteLine($"Usage: sqlparser <file|dir> [{Flag.Lexer}] [{Flag.Parser}] [{Flag.Format}] [{Flag.Stats}] [{Flag.RepeatPrefix}<n>] [{Flag.Color}] [{Flag.NoColor}]");
    output.WriteLine("A file with no flags prints formatted SQL. A directory formats .sql files in place.");
    output.WriteLine($"{Flag.RepeatPrefix}<n> formats a file n times and prints mean time (no SQL output).");
}

static class Flag
{
    public const string Lexer = "--lexer";
    public const string Parser = "--parser";
    public const string Format = "--format";
    public const string Stats = "--stats";
    public const string RepeatPrefix = "--repeat=";
    public const string Color = "--color";
    public const string NoColor = "--no-color";
    public const string Help = "--help";
    public const string HelpShort = "-h";
    public const string NoColorVariable = "NO_COLOR";
}

static class StageName
{
    public const string Lexer = "lexer";
    public const string Parser = "parser";
    public const string Format = "format";
    public const string Stats = "stats";
    public const string ParseFailed = "(parse failed)";
    public const string FilePrefix = "File: ";
}
