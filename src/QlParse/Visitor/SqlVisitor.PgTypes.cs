namespace QlParse;

public partial class SqlVisitor
{
    public virtual void VisitPgDataTypeExtras(PgDataTypeExtras node)
    {
        if (node.IntervalQualifier is not null)
        {
            VisitIntervalQualifier(node.IntervalQualifier);
        }

        foreach (var bound in node.ArrayBounds)
        {
            VisitArrayBound(bound);
        }
    }

    public virtual void VisitArrayBound(ArrayBound node)
    {
        return;
    }
}
