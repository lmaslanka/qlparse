namespace QlParse;

public sealed class CreateTypeStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? UnderKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Supertype { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? Representation { get; init; }
    public SyntaxToken? MembersOpen { get; init; }
    public IReadOnlyList<AttributeDefinition>? Attributes { get; init; }
    public SyntaxToken? MembersClose { get; init; }
    public SyntaxToken? NotInstantiableKeyword { get; init; }
    public SyntaxToken? InstantiableKeyword { get; init; }
    public SyntaxToken? NotFinalKeyword { get; init; }
    public SyntaxToken? FinalKeyword { get; init; }
    public ReferenceGeneration? Reference { get; init; }
    public IReadOnlyList<TypeCastOption> Casts { get; init; } = [];
    public IReadOnlyList<MethodSpecification> Methods { get; init; } = [];
}
