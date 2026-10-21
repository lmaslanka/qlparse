-- expect: Batch(TsqlSetOptionStatement,TsqlIfStatement,TsqlIfStatement,CreateTableStatement,TsqlInsertStatement,SelectStatement,TsqlDeclareStatement,TsqlInsertStatement,SelectStatement,CreateTableStatement,TsqlInsertStatement,SelectStatement,TsqlDropObjectsStatement,TsqlDropObjectsStatement)
SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#who') IS NOT NULL DROP TABLE #who;
IF OBJECT_ID('tempdb..##LockReport') IS NOT NULL DROP TABLE ##LockReport;

CREATE TABLE #who
(
    SPID int, Status varchar(30), [Login] sysname, HostName sysname, BlkBy varchar(10),
    DBName sysname NULL, Command varchar(64), CPUTime int, DiskIO bigint, LastBatch varchar(30),
    ProgramName varchar(128), SPID2 int, REQUESTID int
);

INSERT INTO #who
EXEC sp_who2;

SELECT w.SPID, w.[Login], w.HostName, w.BlkBy, w.DBName, w.Command
INTO ##LockReport
FROM #who AS w
WHERE LTRIM(RTRIM(w.BlkBy)) NOT IN ('', '.');

DECLARE @Sessions TABLE (session_id smallint PRIMARY KEY, wait_type nvarchar(60), wait_time int, sql_text nvarchar(max));

INSERT INTO @Sessions (session_id, wait_type, wait_time, sql_text)
SELECT r.session_id, r.wait_type, r.wait_time, t.text
FROM sys.dm_exec_requests AS r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE r.session_id <> @@SPID
  AND r.session_id > 50;

SELECT lr.*, s.wait_type, s.wait_time, LEFT(s.sql_text, 200) AS sql_snippet
FROM ##LockReport AS lr
LEFT JOIN @Sessions AS s ON s.session_id = lr.SPID;

-- results of a system proc into a temp table with an explicit column list
CREATE TABLE #space (name sysname, [rows] varchar(20), reserved varchar(20), data varchar(20), index_size varchar(20), unused varchar(20));
INSERT #space (name, [rows], reserved, data, index_size, unused) EXEC sp_spaceused N'dbo.Orders';
SELECT * FROM #space;

DROP TABLE #who;
DROP TABLE ##LockReport;
