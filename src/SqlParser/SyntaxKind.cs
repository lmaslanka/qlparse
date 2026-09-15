namespace SqlParser;

internal enum SyntaxKind
{
    EndOfFile,
    Identifier,
    Number,
    SelectKeyword,
    FromKeyword,
    WhereKeyword,
    AndKeyword,
    OrKeyword,
    Comma,
    EqualsToken,
    WhitespaceTrivia,
    LineCommentTrivia,
    BlockCommentTrivia,
}
