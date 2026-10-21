namespace QlParse;

// SQL Server data type grammar (workstream B). Every other T-SQL type (NVARCHAR, DATETIME2(n), MONEY,
// UNIQUEIDENTIFIER, GEOGRAPHY, VECTOR(n), ...) is an ordinary named type with an optional length.
internal sealed partial class Parser
{
    // The length of `VARCHAR(MAX)`, `NVARCHAR(MAX)` and `VARBINARY(MAX)`: the MAX word sits where ISO has a number.
    internal SyntaxToken ExpectTypeLength() =>
        IsSqlServer && IdentifierEquals(Keyword.Max) ? Advance() : Expect(SyntaxKind.Number);

    // The element type of `VECTOR(n, float16)` sits where ISO has the scale.
    internal SyntaxToken ExpectTypeScale() =>
        IsSqlServer && (IdentifierEquals(Keyword.TsqlExprFloat16) || IdentifierEquals(Keyword.TsqlExprFloat32))
            ? Advance()
            : Expect(SyntaxKind.Number);

    // Typed XML: `XML([CONTENT | DOCUMENT] [db.]schema.collection)`.
    internal DataType ParseTsqlXmlType(SyntaxToken name)
    {
        var openParen = Advance();
        SyntaxToken? modifier = (IdentifierEquals(Keyword.Content) || IdentifierEquals(Keyword.Document))
            && NextKind == SyntaxKind.Identifier
            ? Advance()
            : null;
        var collection = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        collection.AddRange(ParseDottedNameTail());
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new DataType
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Modifier = modifier,
            SqlServer = new TsqlDataTypeExtras { SchemaCollection = collection },
        };
    }
}
