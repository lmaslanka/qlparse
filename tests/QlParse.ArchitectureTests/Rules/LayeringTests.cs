using QlParse.ArchitectureTests.Support;

namespace QlParse.ArchitectureTests.Rules;

public sealed class LayeringTests
{
    private static readonly Rule A1 = new(
        "A1",
        "Parser/ and Lexing/ types are internal implementation details",
        "Remove the public modifier - only Sql.cs may expose the parsing pipeline.");

    private static readonly Rule A2 = new(
        "A2",
        "Everything outside Parser/ and Lexing/ is public API surface",
        "Make the type public - Syntax/, Visitor/, SyntaxNodes/, and the root are the library's public contract.");

    private static readonly Rule A3 = new(
        "A3",
        "SyntaxNodes/ must not reference Parser/Lexing types",
        "AST node types must stay pure data holders; move any parsing logic out of SyntaxNodes/.");

    private static readonly Rule A4 = new(
        "A4",
        "Visitor/ must not reference Parser/Lexing types",
        "SqlVisitor only walks an already-built AST; it must not depend on how that AST was produced.");

    private static readonly Rule A5 = new(
        "A5",
        "Parser/ must not reference Visitor types",
        "Dependency direction is one-way (lex -> parse -> visit); move any visitor-aware logic out of Parser/.");

    private static readonly IReadOnlySet<string> ParserAndLexingTypeNames =
        SourceLayout.TypeNamesInFolder("Parser")
            .Concat(SourceLayout.TypeNamesInFolder("Lexing"))
            .ToHashSet(StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> VisitorTypeNames = SourceLayout.TypeNamesInFolder("Visitor");

    [Fact]
    public void A1_ParserAndLexingTypesAreInternal()
    {
        var violations =
            from type in typeof(Sql).Assembly.GetTypes()
            where ParserAndLexingTypeNames.Contains(type.Name) && type.IsPublic
            select $"{type.FullName} is public";

        RuleAssert.Check(A1, violations);
    }

    [Fact]
    public void A2_EverythingElseIsPublic()
    {
        var violations =
            from folder in new[] { "Syntax", "Visitor", "SyntaxNodes", "" }
            from typeName in SourceLayout.TypeNamesInFolder(folder)
            let type = typeof(Sql).Assembly.GetType($"QlParse.{typeName}")
            where type is not null && !type.IsPublic
            select $"{type.FullName} ({(folder.Length == 0 ? "root" : folder)}) is not public";

        RuleAssert.Check(A2, violations);
    }

    [Fact]
    public void A3_SyntaxNodesDoNotReferenceParserOrLexing()
    {
        var violations =
            from file in SourceLayout.InFolder("SyntaxNodes")
            from name in SourceLayout.ReferencedForbiddenNames(file, ParserAndLexingTypeNames)
            select $"{RelativePath(file)} references {name}";

        RuleAssert.Check(A3, violations);
    }

    [Fact]
    public void A4_VisitorDoesNotReferenceParserOrLexing()
    {
        var violations =
            from file in SourceLayout.InFolder("Visitor")
            from name in SourceLayout.ReferencedForbiddenNames(file, ParserAndLexingTypeNames)
            select $"{RelativePath(file)} references {name}";

        RuleAssert.Check(A4, violations);
    }

    [Fact]
    public void A5_ParserDoesNotReferenceVisitor()
    {
        var violations =
            from file in SourceLayout.InFolder("Parser")
            from name in SourceLayout.ReferencedForbiddenNames(file, VisitorTypeNames)
            select $"{RelativePath(file)} references {name}";

        RuleAssert.Check(A5, violations);
    }

    private static string RelativePath(SourceFile file) => Path.GetRelativePath(SourceLayout.SrcRoot, file.Path);
}
