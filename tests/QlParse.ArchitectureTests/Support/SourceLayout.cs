using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QlParse.ArchitectureTests.Support;

internal static class SourceLayout
{
    public static readonly string SrcRoot = ResolveSrcRoot();
    public static readonly IReadOnlyList<SourceFile> Files = LoadFiles();

    public static IEnumerable<SourceFile> InFolder(string folder) =>
        Files.Where(file => file.Folder == folder);

    public static IReadOnlySet<string> TypeNamesInFolder(string folder) =>
        InFolder(folder).SelectMany(file => file.DeclaredTypeNames).ToHashSet(StringComparer.Ordinal);

    public static IEnumerable<string> ReferencedForbiddenNames(SourceFile file, IReadOnlySet<string> forbiddenNames) =>
        file.SyntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Select(node => node.Identifier.Text)
            .Where(forbiddenNames.Contains)
            .Distinct(StringComparer.Ordinal);

    private static string ResolveSrcRoot()
    {
        var metadata = typeof(SourceLayout).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "SourceRoot")
            ?? throw new InvalidOperationException("SourceRoot assembly metadata is missing.");
        var testsProjectDirectory = metadata.Value
            ?? throw new InvalidOperationException("SourceRoot assembly metadata has no value.");
        var repoRoot = Path.GetFullPath(Path.Combine(testsProjectDirectory, "..", ".."));
        return Path.Combine(repoRoot, "src", "QlParse");
    }

    private static IReadOnlyList<SourceFile> LoadFiles()
    {
        var files = new List<SourceFile>();
        foreach (var path in Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(SrcRoot, path);
            var topLevelSegment = relative.Split(Path.DirectorySeparatorChar)[0];
            if (topLevelSegment is "bin" or "obj")
            {
                continue;
            }

            var directory = Path.GetDirectoryName(relative);
            var folder = string.IsNullOrEmpty(directory) ? string.Empty : directory.Split(Path.DirectorySeparatorChar)[0];
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            var typeNames = tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.Text)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            files.Add(new SourceFile(path, folder, tree, typeNames));
        }

        return files;
    }
}
