namespace QlParse;

public partial class SqlVisitor
{
    // Workstream H: ALTER VIEW and ALTER FULLTEXT INDEX.
    internal bool TryVisitTsqlGapsQuery(Query query)
    {
        switch (query)
        {
            case TsqlAlterViewStatement node:
                VisitTsqlAlterViewStatement(node);
                return true;
            case TsqlAlterFulltextIndexStatement node:
                VisitTsqlAlterFulltextIndexStatement(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitTsqlAlterViewStatement(TsqlAlterViewStatement node) => VisitCreateViewStatement(node.View);

    // The option is token-only, like every other SQL Server option.
    public virtual void VisitTsqlAlterFulltextIndexStatement(TsqlAlterFulltextIndexStatement node)
    {
        if (node.Option is not null)
        {
            VisitTsqlOption(node.Option);
        }
    }
}
