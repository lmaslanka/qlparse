using Microsoft.CodeAnalysis;

namespace QlParse.ArchitectureTests.Support;

internal sealed record SourceFile(string Path, string Folder, SyntaxTree SyntaxTree, IReadOnlyList<string> DeclaredTypeNames);
