-- expect: Batch(TsqlExecuteStatement,TsqlReconfigureStatement,TsqlExecuteStatement,TsqlExecuteStatement,TsqlExecuteStatement,TsqlReconfigureStatement,TsqlDbccStatement,TsqlDbccStatement,TsqlDbccStatement,TsqlDbccStatement,TsqlDbccStatement,TsqlDbccStatement,TsqlAlterDatabaseScopedConfigurationStatement,TsqlAlterDatabaseScopedConfigurationStatement,TsqlUpdateStatisticsStatement,TsqlUpdateStatisticsStatement,TsqlExecuteStatement,SelectStatement)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'max degree of parallelism', 4;
EXEC sp_configure 'cost threshold for parallelism', 50;
EXEC sp_configure 'optimize for ad hoc workloads', 1;
RECONFIGURE WITH OVERRIDE;

DBCC SQLPERF(LOGSPACE);
DBCC FREEPROCCACHE;
DBCC DROPCLEANBUFFERS;
DBCC CHECKDB (N'SalesDB') WITH NO_INFOMSGS, ALL_ERRORMSGS, PHYSICAL_ONLY;
DBCC SHRINKFILE (N'SalesDB_log', 1024);
DBCC TRACEON (3226, -1);

ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 4;
ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;

UPDATE STATISTICS Sales.SalesOrderHeader WITH FULLSCAN;
UPDATE STATISTICS dbo.Customer IX_Customer_Email WITH SAMPLE 25 PERCENT;
EXEC sp_updatestats;

SELECT TOP (20)
       qs.total_worker_time / qs.execution_count AS avg_cpu,
       qs.execution_count,
       SUBSTRING(st.text, (qs.statement_start_offset / 2) + 1,
                 ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END
                   - qs.statement_start_offset) / 2) + 1) AS statement_text,
       qp.query_plan
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) AS qp
ORDER BY avg_cpu DESC
OPTION (MAXDOP 1);
