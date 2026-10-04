using QlParse.ArchitectureTests.Support;

namespace QlParse.ArchitectureTests.Rules;

public sealed class FileHygieneTests
{
    private static readonly Rule A6 = new(
        "A6",
        "File name matches a type declared in it",
        "Rename the file (or the type) so the file's name - up to the first '.' - matches one of its declared types.");

    private static readonly Rule A7 = new(
        "A7",
        "Concrete SyntaxNodes/ types are sealed",
        "Add the sealed modifier - AST node types are not designed for further inheritance.");

    [Fact]
    public void A6_FileNameMatchesDeclaredType()
    {
        var violations =
            from file in SourceLayout.Files
            let expectedName = Path.GetFileNameWithoutExtension(file.Path).Split('.')[0]
            where !file.DeclaredTypeNames.Contains(expectedName)
            select $"{Path.GetRelativePath(SourceLayout.SrcRoot, file.Path)} declares "
                + $"[{string.Join(", ", file.DeclaredTypeNames)}], expected '{expectedName}'";

        RuleAssert.Check(A6, violations);
    }

    [Fact]
    public void A7_ConcreteSyntaxNodesAreSealed()
    {
        var violations =
            from typeName in SourceLayout.TypeNamesInFolder("SyntaxNodes")
            let type = typeof(Sql).Assembly.GetType($"QlParse.{typeName}")
            where type is not null && !type.IsAbstract && !type.IsSealed
            select $"{type.FullName} is not sealed";

        RuleAssert.Check(A7, violations);
    }
}
