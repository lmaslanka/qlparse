using System.Collections.Immutable;

namespace SqlParser;

internal readonly record struct SyntaxTrivia(SyntaxKind Kind, string Text);

internal readonly record struct SyntaxToken(
    SyntaxKind Kind,
    string Text,
    int Position,
    ImmutableArray<SyntaxTrivia> LeadingTrivia);
