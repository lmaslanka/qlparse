namespace QlParse;

// `column [TYPE COLUMN type_column] [LANGUAGE language] [STATISTICAL_SEMANTICS]` in CREATE FULLTEXT INDEX.
public sealed class TsqlFulltextIndexColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public SyntaxToken? TypeColumn { get; init; }
    public SyntaxToken? LanguageKeyword { get; init; }
    public SyntaxToken? Language { get; init; }
    public SyntaxToken? StatisticalSemanticsKeyword { get; init; }
}
