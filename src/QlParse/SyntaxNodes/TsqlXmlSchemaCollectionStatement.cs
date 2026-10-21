namespace QlParse;

// CREATE XML SCHEMA COLLECTION name AS expr / ALTER XML SCHEMA COLLECTION name ADD expr;
// VerbKeyword is CREATE or ALTER and ContentKeyword is AS or ADD.
public sealed class TsqlXmlSchemaCollectionStatement : Query
{
    public required SyntaxToken VerbKeyword { get; init; }
    public required SyntaxToken XmlKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required SyntaxToken CollectionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ContentKeyword { get; init; }
    public required Expression Content { get; init; }
}
