-- expect: Script(TsqlIfStatement | TsqlCreateSchemaStatement | TsqlCreateSynonymStatement,TsqlCreateSynonymStatement | TsqlAlterSchemaTransferStatement | CreateTableStatement | TsqlExecuteStatement,TsqlExecuteStatement | SelectStatement)
IF SCHEMA_ID(N'reporting') IS NULL
    EXEC (N'CREATE SCHEMA reporting AUTHORIZATION dbo');
GO
CREATE SCHEMA archive AUTHORIZATION dbo
    CREATE TABLE OrderArchive (OrderID int NOT NULL PRIMARY KEY, ArchivedAt datetime2(0) NOT NULL)
    GRANT SELECT ON SCHEMA::archive TO ReportingRole;
GO
CREATE SYNONYM reporting.Customers FOR [SalesDB].[dbo].[Customer];
CREATE SYNONYM dbo.RemoteOrders FOR [LINKEDSRV01].[ERP].[dbo].[Orders];
GO
ALTER SCHEMA reporting TRANSFER dbo.vw_CustomerContact;
GO
CREATE TABLE reporting.DailyKpi
(
    KpiDate      date NOT NULL,
    Metric       varchar(50) NOT NULL,
    [value]      decimal(19,4) NOT NULL,
    Target       decimal(19,4) NULL,
    Variance     AS ([value] - Target),
    VariancePct  AS (CASE WHEN Target = 0 THEN NULL ELSE ([value] - Target) / Target * 100 END) PERSISTED,
    [year]       AS (DATEPART(YEAR, KpiDate)) PERSISTED,
    [month]      AS (DATEPART(MONTH, KpiDate)) PERSISTED,
    CONSTRAINT PK_DailyKpi PRIMARY KEY CLUSTERED (KpiDate, Metric)
);
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Daily KPI snapshot populated by the ETL.',
    @level0type = N'SCHEMA', @level0name = N'reporting',
    @level1type = N'TABLE',  @level1name = N'DailyKpi';
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Actual minus target.',
    @level0type = N'SCHEMA', @level0name = N'reporting',
    @level1type = N'TABLE',  @level1name = N'DailyKpi',
    @level2type = N'COLUMN', @level2name = N'Variance';
GO
SELECT year, month, Metric, SUM(value) AS total
FROM reporting.DailyKpi
GROUP BY year, month, Metric
ORDER BY year DESC, month DESC;
GO
