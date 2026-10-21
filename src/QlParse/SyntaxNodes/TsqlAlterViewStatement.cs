namespace QlParse;

// SQL Server `ALTER VIEW` redefines the whole view with the CREATE VIEW grammar, so View is the ordinary view
// definition parsed from the ALTER keyword on (its CreateKeyword holds that ALTER token).
public sealed class TsqlAlterViewStatement : Query
{
    public required CreateViewStatement View { get; init; }
}
