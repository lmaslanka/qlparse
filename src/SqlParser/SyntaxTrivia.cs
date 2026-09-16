namespace SqlParser;

internal readonly record struct SyntaxTrivia(SyntaxKind Kind, int Position, int Length);
