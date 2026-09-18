namespace QlParse;

public readonly record struct SyntaxTrivia(SyntaxKind Kind, int Position, int Length);
