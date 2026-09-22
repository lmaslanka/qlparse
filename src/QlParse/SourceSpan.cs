namespace QlParse;

public readonly record struct SourceSpan(int Position, int Length)
{
    public int End => Position + Length;

    public static SourceSpan From(SourceSpan start, SourceSpan end) =>
        new(start.Position, end.End - start.Position);

    public static SourceSpan From(SyntaxToken start, SyntaxToken end) =>
        From(start.Span, end.Span);

    public static SourceSpan From(SyntaxToken start, SourceSpan end) =>
        From(start.Span, end);

    public static SourceSpan From(SourceSpan start, SyntaxToken end) =>
        From(start, end.Span);
}
