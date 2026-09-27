namespace QlParse;

public sealed class ModuleProcedure : Query
{
    public required SyntaxToken ProcedureKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Parameters { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? Semicolon { get; init; }
    public required Query Statement { get; init; }
    public SyntaxToken? StatementSemicolon { get; init; }
}
