using System.Runtime.CompilerServices;

namespace QlParse;

// Under SQL Server, CROSS APPLY / OUTER APPLY is a join whose JoinType is the CROSS or OUTER token and whose
// JoinKeyword is the APPLY identifier, with no constraint.
public sealed class JoinClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NaturalKeyword { get; init; }
    public SyntaxToken? JoinType { get; init; }
    public SyntaxToken? OuterKeyword { get; init; }
    public required SyntaxToken JoinKeyword { get; init; }
    public required TableSource Table { get; init; }

    public JoinConstraint? Constraint
    {
        get => _constraint is TsqlJoinExtras extras ? extras.Constraint : Unsafe.As<JoinConstraint?>(_constraint);
        init => _constraint ??= value;
    }

    // SQL Server join hint; when set it also holds the constraint.
    public TsqlJoinExtras? SqlServer
    {
        get => _constraint as TsqlJoinExtras;
        init
        {
            if (value is not null)
            {
                _constraint = value;
            }
        }
    }

    // Holds the JoinConstraint, or a TsqlJoinExtras carrying it, so hinted SQL Server joins need no extra
    // slot and ISO joins keep their size.
    private readonly object? _constraint;
}
