namespace QlParse;

// `ALTER FULLTEXT STOPLIST name {ADD 'word' LANGUAGE language | DROP {'word' LANGUAGE language |
// ALL LANGUAGE language | ALL}}`.
public sealed class TsqlAlterFulltextStoplistStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken FulltextKeyword { get; init; }
    public required SyntaxToken StoplistKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Action { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? Word { get; init; }
    public SyntaxToken? LanguageKeyword { get; init; }
    public SyntaxToken? Language { get; init; }
}
