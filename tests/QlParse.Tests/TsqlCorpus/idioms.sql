-- One case per "-- case:" line: the expected top-level shape, then the T-SQL text.
-- case: Batch(TsqlTransactionStatement,TsqlUpdateStatement,TsqlTransactionStatement,SelectStatement)
BEGIN TRAN t1 UPDATE t SET a = 1 COMMIT TRAN t1 SELECT 1
-- case: Batch(TsqlIfStatement,SelectStatement)
IF @x = 1 BEGIN SELECT 1 END ELSE BEGIN SELECT 2 END SELECT 3
-- case: Batch(TsqlWhileStatement,SelectStatement)
WHILE @i < 10 SET @i += 1 SELECT @i
-- case: Batch(SelectStatement,SelectStatement)
SELECT 1 -- comment
SELECT 2
-- case: Batch(TsqlDeclareStatement,SelectStatement)
DECLARE @a int, @b int SELECT @a = 1, @b = 2
-- case: Batch(TsqlUpdateStatement,TsqlDeleteStatement)
UPDATE t SET a = 1 WHERE b = 2 DELETE FROM t
-- case: Batch(TsqlInsertStatement,TsqlInsertStatement)
INSERT INTO t VALUES (1) INSERT INTO t VALUES (2)
-- case: Batch(TsqlInsertStatement,SelectStatement)
INSERT INTO t SELECT 1 SELECT 2
-- case: Batch(TsqlExecuteStatement,TsqlExecuteStatement)
EXEC p EXEC q
-- case: Batch(TsqlExecuteStatement,SelectStatement)
EXEC p 1, 2 SELECT 3
-- case: Batch(TsqlExecuteStatement,SelectStatement)
EXEC @rc = dbo.p @a = 1, @b = @c OUTPUT SELECT @rc
-- case: Batch(TsqlPrintStatement,TsqlPrintStatement)
PRINT 1 PRINT 2
-- case: Batch(TsqlSetOptionStatement,SelectStatement)
SET NOCOUNT ON SELECT 1
-- case: Batch(TsqlRaiserrorStatement,TsqlReturnStatement)
RAISERROR('x', 16, 1) RETURN
-- case: Batch(SelectStatement,SelectStatement)
SELECT * FROM t t2 SELECT 1
-- case: Batch(SelectStatement,SelectStatement)
SELECT 1 x SELECT 2
-- case: Batch(TsqlTransactionStatement,SelectStatement)
COMMIT SELECT 1
-- case: Batch(TsqlTransactionStatement,SelectStatement)
ROLLBACK TRANSACTION SELECT 1
-- case: Batch(TsqlTransactionStatement,SelectStatement)
SAVE TRAN sp SELECT 1
-- case: Batch(TsqlWhileStatement,SelectStatement)
WHILE 1 = 1 BEGIN IF @x > 1 BREAK CONTINUE END SELECT 1
-- case: Batch(TsqlGotoStatement,TsqlLabelStatement,TsqlPrintStatement)
GOTO lbl lbl: PRINT 1
-- case: Batch(TsqlCursorStatement,TsqlCursorStatement,SelectStatement)
CLOSE c DEALLOCATE c SELECT 1
-- case: Batch(TsqlCursorStatement,TsqlFetchStatement,SelectStatement)
OPEN c FETCH NEXT FROM c INTO @a SELECT @a
-- case: Batch(TsqlUseStatement,SelectStatement)
USE db SELECT 1
-- case: Batch(TruncateStatement,SelectStatement)
TRUNCATE TABLE t SELECT 1
-- case: Batch(TsqlDropObjectsStatement,CreateTableStatement)
DROP TABLE t CREATE TABLE t (a int)
-- case: Batch(TsqlReturnStatement,SelectStatement)
RETURN 0 SELECT 1
-- case: Batch(TsqlReturnStatement,SelectStatement)
RETURN @x + 1 SELECT 2
-- case: Batch(TsqlWaitforStatement,SelectStatement)
WAITFOR DELAY @d SELECT 1
-- case: Batch(TsqlIfStatement,SelectStatement)
IF @x = 1 RETURN SELECT 2
-- case: Batch(SelectStatement,TsqlThrowStatement)
SELECT 1; THROW 50000, 'x', 1
-- case: Batch(TsqlTryCatchStatement,SelectStatement)
BEGIN TRY SELECT 1 END TRY BEGIN CATCH THROW END CATCH SELECT 2
-- case: Batch(TsqlDeclareStatement,TsqlSetVariableStatement,TsqlCursorStatement)
DECLARE @c CURSOR SET @c = CURSOR FAST_FORWARD FOR SELECT 1 OPEN @c
-- case: Batch(TsqlDeclareCursorStatement,TsqlCursorStatement)
DECLARE c CURSOR LOCAL FOR SELECT a FROM t FOR UPDATE OF a OPEN c
-- case: Batch(TsqlLabelStatement,TsqlTransactionStatement,TsqlReturnStatement)
ErrorHandler: ROLLBACK RETURN -1
-- case: Batch(TsqlIfStatement,TsqlPrintStatement)
IF EXISTS (SELECT 1 FROM t) AND NOT EXISTS (SELECT 1 FROM u) PRINT 1 ELSE IF @x = 2 PRINT 2 ELSE PRINT 3 PRINT 4
-- case: Batch(TsqlSetOptionStatement,SelectStatement)
SET XACT_ABORT, NOCOUNT ON SELECT 1
-- case: Batch(TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement)
SET DATEFORMAT ymd SET LANGUAGE us_english SET DEADLOCK_PRIORITY LOW SET LOCK_TIMEOUT 1000 SET ROWCOUNT 0 SET TEXTSIZE 2048 SET CONTEXT_INFO 0x01
-- case: SelectStatement
SELECT @x = COUNT(*) FROM t
-- case: TsqlInsertStatement
INSERT INTO #t EXEC sp_who
-- case: TsqlRaiserrorStatement
RAISERROR('x %s', 16, 1, @v) WITH NOWAIT
-- case: TsqlIfStatement
IF @@ERROR <> 0 GOTO ErrorHandler
-- case: SelectStatement
SELECT TOP (@n) PERCENT WITH TIES a FROM t ORDER BY a
-- case: TsqlUpdateStatement
UPDATE TOP (10) t SET a = 1
-- case: TsqlDeleteStatement
DELETE t FROM t INNER JOIN u ON t.id = u.id
-- case: SelectStatement
SELECT * FROM OPENQUERY(srv, 'select 1')
-- case: SelectStatement
SELECT CONVERT(varchar(10), GETDATE(), 120), CAST(x AS decimal(18,2))
-- case: CreateTableStatement
CREATE TABLE t (id int IDENTITY(1,1) NOT FOR REPLICATION PRIMARY KEY, rv rowversion, c AS (a+b) PERSISTED, d datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL, e datetime2(7) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL, PERIOD FOR SYSTEM_TIME (d, e))
-- case: SelectStatement
SELECT a FROM t WITH (INDEX(0))
-- case: SelectStatement
SELECT * FROM t1, t2 WHERE t1.a *= t2.a
-- case: TsqlPrintStatement
PRINT 'Done: ' + CAST(@n AS varchar(10))
-- case: TsqlSetVariableStatement
SET @sql += N'x'
-- case: SelectStatement
SELECT 1 AS [Order], 2 AS "Group", 3 AS 'Key'
-- case: SelectStatement
SELECT name, type, type_desc FROM sys.objects WHERE type IN ('U','V')
-- case: SelectStatement
SELECT [key], value, type FROM OPENJSON(@j)
-- case: SelectStatement
SELECT user_name(), SUSER_SNAME(), HOST_NAME(), APP_NAME(), DB_NAME()
-- case: SelectStatement
SELECT IIF(a > 1, 'x', 'y'), CHOOSE(2, 'a', 'b')
-- case: SelectStatement
SELECT * FROM t WHERE CONTAINS(col, '"word*"')
-- case: SelectStatement
SELECT COUNT(DISTINCT a), STRING_AGG(b, ',') WITHIN GROUP (ORDER BY b) FROM t
-- case: SelectStatement
SELECT * FROM [server].[db].[dbo].[t]
-- case: TsqlExecuteStringStatement
EXEC ('select 1') AT linked_server
-- case: SelectStatement
SELECT $IDENTITY FROM t
-- case: CreateIndexStatement
CREATE INDEX ix ON t(a) WHERE b IS NOT NULL WITH (ONLINE = ON) ON ps(a)
-- case: AlterTableStatement
ALTER TABLE t ADD CONSTRAINT df DEFAULT 0 FOR c
-- case: AlterTableStatement
ALTER TABLE t ALTER COLUMN c nvarchar(100) NULL
-- case: AlterTableStatement
ALTER TABLE t DROP COLUMN IF EXISTS c
-- case: AlterTableStatement
ALTER TABLE t NOCHECK CONSTRAINT ALL
-- case: TruncateStatement
TRUNCATE TABLE t WITH (PARTITIONS (1 TO 3))
-- case: TsqlDbccStatement
DBCC CHECKIDENT ('t', RESEED, 0)
-- case: TsqlDeclareStatement
DECLARE @t TABLE (id int PRIMARY KEY, name nvarchar(50) INDEX ix NONCLUSTERED)
-- case: TsqlCreateTypeStatement
CREATE TYPE dbo.IdList AS TABLE (Id int NOT NULL PRIMARY KEY)
-- case: TsqlWithXmlNamespacesQuery
WITH XMLNAMESPACES ('uri' AS ns) SELECT 1
-- case: SelectStatement
SELECT x.value('(/a/b)[1]', 'int') FROM @xml.nodes('/a') AS t(x)
-- case: SelectStatement
SELECT NEXT VALUE FOR dbo.seq
-- case: SelectStatement
SELECT * INTO #t FROM t WHERE 1 = 0
-- case: SelectStatement
SELECT * FROM t ORDER BY 1 DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY OPTION (RECOMPILE)
-- case: SelectStatement
SELECT * FROM t AS a (NOLOCK) INNER LOOP JOIN u WITH (NOLOCK) ON a.id = u.id
-- case: SelectStatement
SELECT * FROM t a (NOLOCK) JOIN u b (NOLOCK) ON a.id = b.id
-- case: TsqlUpdateStatisticsStatement
UPDATE STATISTICS t WITH FULLSCAN
-- case: TsqlCreateStatisticsStatement
CREATE STATISTICS s ON t (a) WITH FULLSCAN
-- case: SelectStatement
SELECT * FROM t WHERE c = @p OPTION (OPTIMIZE FOR (@p UNKNOWN))
-- case: SelectStatement
SELECT SYSDATETIME(), SYSUTCDATETIME(), GETUTCDATE(), FORMAT(d, 'yyyy-MM-dd', 'en-US'), DATEFROMPARTS(2020,1,1)
-- case: SelectStatement
SELECT CAST(d AS date) AS [date], CONVERT(date, d) date, a time, position FROM t ORDER BY position, date
-- case: TsqlDeclareStatement
DECLARE @position int, @date date, @time time(0), @name sysname, @filter nvarchar(10)
-- case: Batch(CreateTableStatement,CreateTableStatement,CreateTableStatement)
CREATE TABLE dbo.[User] (id int) CREATE TABLE dbo.Filter (id int) CREATE TABLE dbo.Window (id int)
-- case: SelectStatement
SELECT * FROM dbo.Match m WHERE m.match = 1
-- case: SelectStatement
SELECT * FROM t FOR SYSTEM_TIME ALL
-- case: TsqlMergeStatement
MERGE dbo.t AS tgt USING (SELECT a FROM s) AS src (a) ON tgt.a = src.a WHEN NOT MATCHED BY TARGET THEN INSERT (a) VALUES (src.a) WHEN NOT MATCHED BY SOURCE THEN DELETE OUTPUT $action, inserted.a INTO @log;
-- case: TsqlInsertStatement
INSERT INTO t DEFAULT VALUES
-- case: TsqlInsertStatement
INSERT t (a) OUTPUT inserted.id INTO @ids SELECT a FROM u
-- case: SelectStatement
SELECT * FROM sys.dm_exec_requests r CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) st
-- case: TsqlPermissionStatement
GRANT SELECT ON OBJECT::dbo.t TO [role] WITH GRANT OPTION
-- case: TsqlAlterAuthorizationStatement
ALTER AUTHORIZATION ON SCHEMA::s TO dbo
-- case: TsqlCreatePrincipalStatement
CREATE LOGIN [DOMAIN\user] FROM WINDOWS WITH DEFAULT_DATABASE=[master]
-- case: SelectStatement
SELECT * FROM t TABLESAMPLE (10 PERCENT)
-- case: SelectStatement
SELECT a FROM t GROUP BY a WITH ROLLUP
-- case: SelectStatement
SELECT * FROM t WHERE a LIKE '[a-c]%' ESCAPE '\'
-- case: SelectStatement
SELECT 1 WHERE 'a' COLLATE Latin1_General_CS_AS = 'A'
-- case: TsqlCreateProcedureStatement
CREATE PROCEDURE p AS SET NOCOUNT ON; SELECT 1
-- case: TsqlCreateFunctionStatement
CREATE FUNCTION f() RETURNS int AS BEGIN RETURN 1 END
-- case: TsqlSecurityObjectStatement
OPEN SYMMETRIC KEY k DECRYPTION BY CERTIFICATE c
-- case: TsqlWaitforStatement
WAITFOR (RECEIVE TOP(1) * FROM q), TIMEOUT 5000
-- case: SelectStatement
SELECT 0x1F, $10.5, 1e5, .5, -1, ~1, 5 % 2, 1 & 3 | 4 ^ 2
-- case: SelectStatement
SELECT * FROM tempdb..sysobjects
-- case: SelectStatement
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), SUM(x) OVER (PARTITION BY y ORDER BY z ROWS UNBOUNDED PRECEDING) FROM t
-- case: SelectStatement
SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY x) OVER (PARTITION BY y) FROM t
-- case: SelectStatement
SELECT FIRST_VALUE(x) IGNORE NULLS OVER (ORDER BY y) FROM t
-- case: SelectStatement
SELECT GREATEST(1,2), LEAST(1,2), DATE_BUCKET(day, 1, d), DATETRUNC(month, d) FROM t
-- case: SelectStatement
SELECT value FROM GENERATE_SERIES(1, 10)
-- case: TsqlCreateSpecialIndexStatement
CREATE CLUSTERED COLUMNSTORE INDEX cci ON t WITH (DROP_EXISTING = ON)
-- case: TsqlAlterIndexStatement
ALTER INDEX ALL ON t REBUILD WITH (ONLINE = ON (WAIT_AT_LOW_PRIORITY (MAX_DURATION = 1 MINUTES, ABORT_AFTER_WAIT = SELF)))
-- case: TsqlAlterDatabaseStatement
ALTER DATABASE [db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
-- case: Batch(TsqlCheckpointStatement,TsqlKillStatement)
CHECKPOINT KILL 52
-- case: SelectStatement
SELECT @@VERSION, @@SERVERNAME, @@SPID
-- case: TsqlExecuteStatement
EXEC sys.sp_executesql N'SELECT @x', N'@x int', 1
-- case: SelectStatement
SELECT year = YEAR(d), [month] = MONTH(d) FROM t
-- case: SetOperation
SELECT a FROM t1 UNION ALL SELECT a FROM t2 ORDER BY a
-- case: SelectStatement
SELECT * FROM t WHERE d BETWEEN @a AND @b
-- case: SelectStatement
SELECT 'a' + CHAR(13) + CHAR(10) + 'b'
-- case: SelectStatement
SELECT * FROM t WHERE t.date >= DATEADD(day, -7, CAST(GETDATE() AS date))
-- case: Batch(CreateTableStatement,TsqlInsertStatement,TsqlDropObjectsStatement)
CREATE TABLE #t (id int) INSERT #t SELECT 1 DROP TABLE #t
-- case: SelectStatement
SELECT TOP 10 * FROM t ORDER BY NEWID()
-- case: SelectStatement
SELECT TOP 1 WITH TIES a FROM t ORDER BY a
-- case: SelectStatement
SELECT DISTINCT TOP 5 a FROM t
-- case: TsqlDeleteStatement
DELETE TOP (1000) FROM dbo.Log WHERE d < @c
-- case: TsqlUpdateStatement
UPDATE t SET @x = a = a + 1
-- case: SelectStatement
SELECT * FROM t WHERE a IN (SELECT a FROM u UNION SELECT b FROM v)
-- case: SelectStatement
SELECT a, (SELECT TOP 1 b FROM u WHERE u.id = t.id ORDER BY b) AS b FROM t
-- case: SelectStatement
SELECT * FROM (SELECT a FROM t) AS d (x)
-- case: SelectStatement
SELECT * FROM (VALUES (1, 'a'), (2, 'b')) AS v (id, name)
-- case: SelectStatement
SELECT STUFF((SELECT ',' + name FROM t FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, '')
-- case: TsqlExecuteStatement
EXEC dbo.p @d = '2020-01-01', @flag = DEFAULT
-- case: Batch(TsqlExecuteAsStatement,SelectStatement,TsqlRevertStatement)
EXECUTE AS USER = 'x' SELECT 1 REVERT
-- case: SelectStatement
SELECT * FROM t WHERE x = N'O''Brien'
-- case: SelectStatement
SELECT CASE a WHEN 1 THEN 'x' ELSE 'y' END [Status] FROM t
-- case: TsqlCreateTriggerStatement
CREATE OR ALTER TRIGGER trg ON dbo.t AFTER INSERT, UPDATE NOT FOR REPLICATION AS IF UPDATE(a) OR COLUMNS_UPDATED() & 2 > 0 BEGIN SELECT 1 END
-- case: TsqlToggleTriggerStatement
ENABLE TRIGGER ALL ON DATABASE
-- case: TsqlToggleTriggerStatement
DISABLE TRIGGER trg ON dbo.t
-- case: SelectStatement
SELECT a.b.c.d FROM t
-- case: SelectStatement
SELECT * FROM t WHERE x IS NULL ORDER BY x COLLATE Latin1_General_BIN
-- case: SelectStatement
SELECT CAST(1 AS bit) IsActive, CAST(N'x' AS nvarchar(max)) AS v
-- case: TsqlDeclareStatement
DECLARE @j nvarchar(max) = (SELECT a FROM t FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
-- case: SelectStatement
SELECT * FROM OPENJSON(@j) WITH (id int '$.id', name nvarchar(50) '$.name', tags nvarchar(max) '$.tags' AS JSON)
-- case: SelectStatement
SELECT JSON_VALUE(@j, '$.a'), JSON_QUERY(@j, '$.b'), JSON_MODIFY(@j, 'append $.c', 1), ISJSON(@j)
-- case: Batch(CreateTableStatement,SelectStatement)
CREATE TABLE t (v VECTOR(3) NOT NULL) SELECT VECTOR_DISTANCE('cosine', v, CAST('[1,2,3]' AS VECTOR(3))) FROM t
-- case: SelectStatement
SELECT * FROM t WHERE MATCH(SHORTEST_PATH(a(-(e)->b)+))
-- case: SelectStatement
SELECT * FROM a, e, b WHERE MATCH(a-(e)->b) AND a.name = 'x'
-- case: TsqlInsertStatement
INSERT INTO e ($from_id, $to_id) VALUES ((SELECT $node_id FROM a WHERE id = 1), (SELECT $node_id FROM b WHERE id = 2))
-- case: TsqlBulkInsertStatement
BULK INSERT dbo.t FROM 'c:\x.csv' WITH (FIRSTROW = 2, FIELDTERMINATOR = ',', ROWTERMINATOR = '\n', TABLOCK)
-- case: TsqlBackupStatement
BACKUP DATABASE db TO DISK = N'c:\b.bak' WITH INIT, COMPRESSION, STATS = 10
-- case: TsqlRestoreStatement
RESTORE DATABASE db FROM DISK = N'c:\b.bak' WITH MOVE 'db' TO 'c:\db.mdf', REPLACE, RECOVERY
-- case: SelectStatement
SELECT * FROM t WHERE a = 1 OPTION (MAXDOP 1, RECOMPILE)
-- case: SelectStatement
SELECT * FROM t1 LEFT OUTER JOIN t2 ON t1.a = t2.a FULL JOIN t3 ON 1 = 1 CROSS JOIN t4
-- case: SelectStatement
SELECT * FROM t1 LEFT MERGE JOIN t2 ON t1.a = t2.a
-- case: SelectStatement
SELECT CONVERT(VARCHAR(8), GETDATE(), 112) AS today, DATEDIFF_BIG(ms, a, b)
-- case: SelectStatement
SELECT * FROM t WHERE NOT (a = 1 OR b = 2)
-- case: SelectStatement
SELECT * FROM t WHERE a !< 1 AND b !> 2 AND c != 3
-- case: Script( | SelectStatement)
GO
SELECT 1
GO 2
-- case: Script(SelectStatement)
SELECT 1
GO
-- trailing comment
-- case: Script(TsqlSetOptionStatement | TsqlSetOptionStatement)
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
-- case: SelectStatement
SELECT TRY_PARSE('1' AS int USING 'en-US'), PARSE('1' AS int)
-- case: SelectStatement
SELECT * FROM t WHERE a = ALL (SELECT b FROM u) OR a > ANY (SELECT b FROM u)
-- case: SelectStatement
SELECT COUNT_BIG(*), AVG(CAST(a AS float)), STDEV(a), VAR(a), CHECKSUM_AGG(a), GROUPING_ID(a, b) FROM t GROUP BY CUBE(a, b)
-- case: SelectStatement
SELECT * FROM t PIVOT (SUM(v) FOR k IN ([a], [b])) AS p
-- case: SelectStatement
SELECT * FROM t UNPIVOT (v FOR k IN (a, b)) AS u
-- case: SelectStatement
SELECT * FROM t1 CROSS APPLY (SELECT TOP (1) * FROM t2 WHERE t2.id = t1.id ORDER BY t2.d DESC) AS x OUTER APPLY STRING_SPLIT(t1.s, ',') AS s
-- case: SelectStatement
SELECT * FROM STRING_SPLIT('a,b', ',', 1)
-- case: SelectStatement
SELECT * FROM t WHERE EXISTS (SELECT * FROM u WHERE u.id = t.id) ORDER BY 1
-- case: SelectStatement
SELECT * FROM t WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE id = @id
-- case: TsqlUpdateStatement
UPDATE t WITH (ROWLOCK) SET a = 1 OUTPUT deleted.a, inserted.a WHERE id = 1
-- case: TsqlDeleteStatement
DELETE FROM t OUTPUT deleted.* INTO #d WHERE id = 1
-- case: SelectStatement
SELECT * FROM t AS t1 WITH (NOLOCK) JOIN t AS t2 WITH (NOLOCK) ON t1.id = t2.pid
-- case: TsqlCreateSynonymStatement
CREATE SYNONYM dbo.s FOR srv.db.dbo.t
-- case: AlterTableStatement
ALTER TABLE dbo.t SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.t_history, DATA_CONSISTENCY_CHECK = ON))
-- case: AlterTableStatement
ALTER TABLE dbo.t ADD PERIOD FOR SYSTEM_TIME (s, e)
-- case: AlterTableStatement
ALTER TABLE dbo.t ADD s datetime2 GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT df_s DEFAULT SYSUTCDATETIME(), e datetime2 GENERATED ALWAYS AS ROW END HIDDEN CONSTRAINT df_e DEFAULT CONVERT(datetime2, '9999-12-31 23:59:59.9999999')
-- case: AlterTableStatement
ALTER TABLE dbo.t WITH CHECK ADD CONSTRAINT FK_x FOREIGN KEY (a) REFERENCES dbo.u (a) ON DELETE CASCADE ON UPDATE NO ACTION
-- case: AlterTableStatement
ALTER TABLE dbo.t CHECK CONSTRAINT FK_x
-- case: TsqlExecuteStatement
EXEC sp_rename 'dbo.t.a', 'b', 'COLUMN'
-- case: SelectStatement
SELECT OBJECT_ID(N'tempdb..#t'), OBJECT_SCHEMA_NAME(object_id), COL_LENGTH('dbo.t', 'c')
-- case: TsqlIfStatement
IF OBJECT_ID('tempdb..#t') IS NOT NULL DROP TABLE #t
-- case: TsqlIfStatement
IF COL_LENGTH('dbo.t', 'c') IS NULL ALTER TABLE dbo.t ADD c int NULL
-- case: TsqlCreateProcedureStatement
CREATE PROCEDURE dbo.p @a int WITH EXECUTE AS OWNER, RECOMPILE AS SELECT @a
-- case: TsqlCreateProcedureStatement
CREATE PROCEDURE dbo.p @a int WITH NATIVE_COMPILATION, SCHEMABINDING AS BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english') SELECT @a END
-- case: CreateTableStatement
CREATE TABLE dbo.t (id int NOT NULL PRIMARY KEY NONCLUSTERED, c nvarchar(10), INDEX ix HASH (c) WITH (BUCKET_COUNT = 1024)) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_ONLY)
-- case: CreateTableStatement
CREATE TABLE t (c int SPARSE NULL, cs XML COLUMN_SET FOR ALL_SPARSE_COLUMNS)
-- case: CreateTableStatement
CREATE TABLE t (a int) WITH (DATA_COMPRESSION = PAGE)
-- case: AlterTableStatement
ALTER TABLE t REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE)
-- case: AlterTableStatement
ALTER TABLE t SWITCH PARTITION 1 TO t2 PARTITION 1
-- case: CreateIndexStatement
CREATE UNIQUE NONCLUSTERED INDEX ix ON t (a DESC) INCLUDE (b) WHERE c = 1 WITH (FILLFACTOR = 90, SORT_IN_TEMPDB = ON) ON [PRIMARY]
-- case: TsqlCreateSpecialIndexStatement
CREATE PRIMARY XML INDEX px ON t (x)
-- case: SelectStatement
SELECT * FROM t WITH (FORCESEEK (ix (a)))
-- case: SelectStatement
SELECT * FROM t OPTION (USE HINT ('DISABLE_OPTIMIZED_PLAN_FORCING'), QUERYTRACEON 9481)
-- case: SelectStatement
SELECT CONVERT(xml, @x).query('/a'), @x.exist('/a')
-- case: TsqlUpdateStatement
UPDATE t SET xmlcol.modify('delete /a')
-- case: SelectStatement
SELECT geography::Point(47.6, -122.3, 4326), hierarchyid::GetRoot(), @h.GetAncestor(1).ToString()
-- case: SelectStatement
SELECT * FROM t WHERE col.STDistance(@p) < 100
-- case: TsqlDeclareStatement
DECLARE @g geography = geography::STGeomFromText('POINT(1 2)', 4326)
-- case: SelectStatement
SELECT CAST(a AS nvarchar(max)) COLLATE DATABASE_DEFAULT FROM t
-- case: SelectStatement
SELECT TOP (100) PERCENT a FROM t ORDER BY a
-- case: TsqlInsertStatement
INSERT INTO t WITH (TABLOCK) SELECT a FROM u
-- case: SelectStatement
SELECT a, b FROM t GROUP BY ALL a, b
-- case: SelectStatement
SELECT a FROM t COMPUTE SUM(a)
-- case: SelectStatement
SELECT * FROM OPENROWSET(BULK 'c:\x.json', SINGLE_CLOB) AS j
-- case: SelectStatement
SELECT * FROM OPENROWSET('SQLNCLI', 'Server=x;Trusted_Connection=yes;', 'SELECT 1')
-- case: SelectStatement
SELECT * FROM OPENDATASOURCE('SQLNCLI', 'Data Source=x').db.dbo.t
-- case: SelectStatement
SELECT * FROM OPENXML(@h, '/root', 2) WITH (a int)
-- case: TsqlExecuteStatement
EXEC sp_xml_preparedocument @h OUTPUT, @xml
-- case: TsqlEventSessionStatement
CREATE EVENT SESSION s ON SERVER ADD EVENT sqlserver.sql_statement_completed (ACTION (sqlserver.sql_text) WHERE duration > 1000) ADD TARGET package0.event_file (SET filename = N'x') WITH (MAX_MEMORY = 4096 KB)
-- case: TsqlEventSessionStatement
ALTER EVENT SESSION s ON SERVER STATE = START
-- case: TsqlCreateDatabaseStatement
CREATE DATABASE db ON PRIMARY (NAME = x, FILENAME = 'c:\x.mdf', SIZE = 8MB, FILEGROWTH = 64MB) LOG ON (NAME = y, FILENAME = 'c:\y.ldf')
-- case: TsqlAlterDatabaseStatement
ALTER DATABASE db MODIFY FILE (NAME = x, SIZE = 100MB)
-- case: TsqlAlterDatabaseStatement
ALTER DATABASE db SET RECOVERY SIMPLE
-- case: TsqlAlterDatabaseStatement
ALTER DATABASE db SET COMPATIBILITY_LEVEL = 160
-- case: TsqlAlterDatabaseStatement
ALTER DATABASE db SET QUERY_STORE = ON (OPERATION_MODE = READ_WRITE)
-- case: TsqlDbccStatement
DBCC SHRINKFILE (N'log', 1) WITH NO_INFOMSGS
-- case: Batch(TsqlDbccStatement,TsqlDbccStatement,TsqlDbccStatement)
DBCC INPUTBUFFER(52) DBCC SQLPERF(LOGSPACE) DBCC TRACEON(1222, -1)
-- case: SelectStatement
SELECT * FROM sys.fn_get_audit_file('x', DEFAULT, DEFAULT)
-- case: SelectStatement
SELECT dbo.f(DEFAULT)
-- case: TsqlExecuteStatement
EXEC dbo.p DEFAULT, 1
-- case: SelectStatement
SELECT CASE WHEN EXISTS (SELECT 1) THEN 1 ELSE 0 END
-- case: TsqlSetVariableStatement
SET @x = (SELECT MAX(a) FROM t)
-- case: SelectStatement
SELECT @@ROWCOUNT AS [RowCount]
-- case: TsqlIfStatement
IF (@x IS NULL) BEGIN RAISERROR(N'x', 16, 1); RETURN; END
-- case: TsqlPrintStatement
PRINT CONVERT(varchar, GETDATE(), 121)
-- case: SelectStatement
SELECT CAST(a AS varchar) FROM t
-- case: SelectStatement
SELECT [x].* FROM [dbo].[t] AS [x] WHERE [x].[a] = 1
-- case: TsqlExecuteStringStatement
EXEC('USE ' + @db + '; SELECT 1')
-- case: TsqlInsertStatement
INSERT INTO @t (a) VALUES (1), (2), (3)
-- case: WithQuery
;WITH cte AS (SELECT 1 AS n UNION ALL SELECT n + 1 FROM cte WHERE n < 10) SELECT * FROM cte OPTION (MAXRECURSION 100)
-- case: Batch(SelectStatement,WithQuery)
SELECT 1
;WITH c AS (SELECT 1 AS a) SELECT a FROM c
-- case: TsqlCreateTriggerStatement
CREATE TRIGGER tr ON dbo.t FOR INSERT AS SELECT 1
-- case: TsqlCreateTriggerStatement
CREATE TRIGGER tr ON ALL SERVER FOR LOGON AS BEGIN SELECT 1 END
-- case: TsqlCreateFunctionStatement
CREATE FUNCTION f(@a int) RETURNS @t TABLE (a int) WITH SCHEMABINDING AS BEGIN INSERT @t SELECT @a RETURN END
-- case: TsqlCreateFunctionStatement
CREATE FUNCTION f() RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS a
-- case: TsqlCreateProcedureStatement
CREATE PROCEDURE p @t dbo.IdList READONLY, @o int = NULL OUT AS SELECT 1
-- case: TsqlCreateProcedureStatement
CREATE PROC p AS RETURN 0
-- case: TsqlCreateProcedureStatement
ALTER PROCEDURE dbo.p AS SELECT 1
-- case: TsqlPermissionStatement
GRANT EXECUTE ON TYPE::dbo.IdList TO r
-- case: TsqlPermissionStatement
DENY VIEW DEFINITION TO r CASCADE
-- case: TsqlPermissionStatement
REVOKE CONNECT FROM guest
-- case: TsqlAlterPrincipalStatement
ALTER LOGIN sa DISABLE
-- case: TsqlAlterPrincipalStatement
ALTER USER u WITH DEFAULT_SCHEMA = dbo
-- case: TsqlCreatePrincipalStatement
CREATE USER u WITHOUT LOGIN
-- case: TsqlAlterPrincipalStatement
ALTER SERVER ROLE sysadmin ADD MEMBER [x]
-- case: TsqlSecurityObjectStatement
CREATE CERTIFICATE c WITH SUBJECT = 'x'
-- case: TsqlSecurityObjectStatement
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'x'
-- case: TsqlSecurityObjectStatement
BACKUP CERTIFICATE c TO FILE = 'x' WITH PRIVATE KEY (FILE = 'y', ENCRYPTION BY PASSWORD = 'z')
-- case: TsqlSignatureStatement
ADD SIGNATURE TO dbo.p BY CERTIFICATE c
-- case: SelectStatement
SELECT ENCRYPTBYPASSPHRASE('k', 'x')
-- case: TsqlSetOptionStatement
SET STATISTICS IO, TIME ON
-- case: TsqlSetOptionStatement
SET SHOWPLAN_XML ON
-- case: Batch(TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlSetOptionStatement)
SET NOEXEC ON SET PARSEONLY ON SET FMTONLY OFF SET ARITHABORT ON SET ANSI_WARNINGS OFF SET CONCAT_NULL_YIELDS_NULL ON SET NUMERIC_ROUNDABORT OFF SET ANSI_PADDING ON SET IMPLICIT_TRANSACTIONS OFF SET CURSOR_CLOSE_ON_COMMIT OFF SET DATEFIRST 1
-- case: TsqlTransactionStatement
BEGIN DISTRIBUTED TRANSACTION
-- case: Batch(TsqlTransactionStatement,TsqlTransactionStatement)
COMMIT WORK ROLLBACK WORK
-- case: TsqlTransactionStatement
BEGIN TRAN @name WITH MARK 'x'
-- case: TsqlUpdateStatement
UPDATE t SET c.WRITE(N'x', 0, 1)
-- case: SelectStatement
SELECT IDENT_CURRENT('t'), SCOPE_IDENTITY(), @@IDENTITY
-- case: SelectStatement
SELECT * FROM t1 INNER REMOTE JOIN t2 ON t1.a = t2.a
-- case: SelectStatement
SELECT * FROM CHANGETABLE(CHANGES dbo.t, @v) AS c
-- case: SelectStatement
SELECT * FROM t WHERE FREETEXT(*, 'x')
-- case: SelectStatement
SELECT * FROM CONTAINSTABLE(t, *, 'x') AS k
-- case: SelectStatement
SELECT LAG(a, 1, 0) OVER (PARTITION BY b ORDER BY c), COUNT(*) OVER () AS total FROM t
-- case: SelectStatement
SELECT a FROM t ORDER BY CASE WHEN a = 1 THEN 0 ELSE 1 END, b DESC
-- case: SelectStatement
SELECT * FROM t WHERE a = 1 FOR BROWSE
-- case: SelectStatement
SELECT * FROM t FOR XML RAW, ELEMENTS XSINIL, ROOT('r')
-- case: SelectStatement
SELECT 1 AS Tag, NULL AS Parent FOR XML EXPLICIT
-- case: SelectStatement
SELECT * FROM t FOR JSON AUTO, INCLUDE_NULL_VALUES
-- case: TsqlWhileStatement
WHILE EXISTS (SELECT 1 FROM t) BEGIN DELETE TOP (100) FROM t END
-- case: SelectStatement
SELECT * FROM t WHERE id = @id;;
-- case: SelectStatement
SELECT a /* c /* nested */ c */ FROM t -- x
GROUP BY a
-- case: Script(SelectStatement |  | SelectStatement)
SELECT 'GO' AS x
-- GO
   GO
go
SELECT 2
-- case: Script(SelectStatement | SelectStatement)
SELECT 1
GO -- comment
SELECT 2
-- case: SelectStatement
SELECT * FROM dbo.t AS t1 WHERE t1.[date] = CONVERT(date, GETDATE())
-- case: TsqlDeclareStatement
DECLARE @tbl TABLE ([date] date, [value] decimal(18, 2), [type] tinyint)
-- case: SelectStatement
SELECT TOP(1) @d = [date] FROM t
-- case: CreateTableStatement
CREATE TABLE dbo.Calendar ([Date] date NOT NULL CONSTRAINT PK_Calendar PRIMARY KEY, [Year] AS YEAR([Date]), [Month] AS MONTH([Date]) PERSISTED)
-- case: SelectStatement
SELECT * FROM t WHERE x = 1 ORDER BY y OFFSET 0 ROWS
-- case: TsqlExecuteStatement
EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'x', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N't'
-- case: SelectStatement
SELECT * FROM t WHERE a = 1 AND (b = 2 OR c = 3) AND d IN (SELECT d FROM u) AND e LIKE 'x%' AND f BETWEEN 1 AND 2
-- case: CreateTableStatement
CREATE TABLE t (a int CONSTRAINT ck CHECK (a > 0), b int NULL DEFAULT (0), c AS a * 2, CONSTRAINT uq UNIQUE NONCLUSTERED (b) WITH (IGNORE_DUP_KEY = OFF))
-- case: CreateTableStatement
CREATE TABLE [dbo].[t] ([id] [int] IDENTITY(1,1) NOT NULL, [name] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL, [rowguid] [uniqueidentifier] ROWGUIDCOL NOT NULL) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
-- case: SelectStatement
SELECT TOP 5 WITH TIES * FROM t ORDER BY a
-- case: SelectStatement
SELECT * FROM t AS a INNER JOIN u AS b WITH (NOLOCK) ON a.id = b.id WHERE a.x = 1 OPTION (HASH JOIN, LOOP JOIN)
-- case: SelectStatement
SELECT DATEPART(dw, GETDATE()), DATENAME(month, GETDATE()), EOMONTH(GETDATE(), -1)
-- case: SelectStatement
SELECT a + b AS [sum], a - b [diff], [prod] = a * b FROM t
-- case: SelectStatement
SELECT N'x' + CAST(1 AS nvarchar(10)) + N'y'
-- case: SelectStatement
SELECT * FROM t WHERE ISNULL(a, 0) = COALESCE(b, c, 0) AND NULLIF(a, 0) IS NOT NULL
-- case: TsqlSecurityObjectStatement
BACKUP CERTIFICATE c TO FILE = 'x' WITH PRIVATE KEY (FILE = 'y', ENCRYPTION BY PASSWORD = 'z')
-- case: TsqlSecurityObjectStatement
BACKUP MASTER KEY TO FILE = 'c:\k' ENCRYPTION BY PASSWORD = 'p'
-- case: TsqlSecurityObjectStatement
BACKUP SERVICE MASTER KEY TO FILE = 'c:\k' ENCRYPTION BY PASSWORD = 'p'
-- case: TsqlSecurityObjectStatement
RESTORE MASTER KEY FROM FILE = 'c:\k' DECRYPTION BY PASSWORD = 'p' ENCRYPTION BY PASSWORD = 'q' FORCE
-- case: TsqlSecurityObjectStatement
RESTORE SERVICE MASTER KEY FROM FILE = 'c:\k' DECRYPTION BY PASSWORD = 'p'
-- case: TsqlSecurityObjectStatement
BACKUP SYMMETRIC KEY k TO FILE = 'c:\k' ENCRYPTION BY PASSWORD = 'p'
-- case: Batch(TsqlBackupStatement,SelectStatement)
BACKUP DATABASE db TO DISK = 'x' SELECT 1
-- case: SelectStatement
SELECT * FROM CHANGETABLE(CHANGES dbo.t, @v) AS c
-- case: SelectStatement
SELECT * FROM CHANGETABLE(VERSION dbo.t, (id), (@id)) AS c
-- case: SelectStatement
SELECT c.id, c.SYS_CHANGE_OPERATION FROM CHANGETABLE(CHANGES dbo.t, 0) AS c JOIN dbo.t ON t.id = c.id
-- case: TsqlIfStatement
IF @a = 1 BEGIN SELECT 1 END ELSE IF @a = 2 BEGIN SELECT 2 END ELSE BEGIN SELECT 3 END
-- case: SetOperation
SELECT * FROM t WHERE a = 1 UNION SELECT * FROM u WHERE b = 2 ORDER BY 1 OPTION (RECOMPILE)
-- case: TsqlInsertStatement
INSERT INTO t (a, b) SELECT a, b FROM u WHERE NOT EXISTS (SELECT 1 FROM t WHERE t.a = u.a)
-- case: TsqlUpdateStatement
UPDATE t SET a = s.a FROM t INNER JOIN s ON s.id = t.id WHERE s.x > 0
-- case: TsqlUpdateStatement
UPDATE t SET a += 1, b = DEFAULT, c = NULL OUTPUT inserted.* WHERE id = 1
-- case: TsqlDeleteStatement
DELETE FROM t WHERE CURRENT OF c
-- case: SelectStatement
SELECT * FROM t WITH (NOLOCK) OPTION (FORCE ORDER)
-- case: SelectStatement
SELECT * FROM t1 a WITH (NOLOCK), t2 b WHERE a.x = b.x
-- case: SelectStatement
SELECT CAST(a AS DECIMAL(10, 2)) / NULLIF(b, 0) * 100.0 AS pct FROM t
-- case: SelectStatement
SELECT DATEADD(MINUTE, DATEDIFF(MINUTE, 0, GETDATE()), 0)
-- case: SelectStatement
SELECT CONVERT(CHAR(10), d, 101), CHAR(65), NCHAR(66), ASCII('a'), UNICODE(N'b') FROM t
-- case: SelectStatement
SELECT LEN(a), DATALENGTH(a), REPLACE(a, 'x', 'y'), REPLICATE('0', 5 - LEN(a)) + a, RIGHT('00' + CAST(n AS varchar(2)), 2) FROM t
-- case: SelectStatement
SELECT * FROM t ORDER BY a OFFSET (@page - 1) * @size ROWS FETCH NEXT @size ROWS ONLY
-- case: SelectStatement
SELECT COALESCE((SELECT TOP 1 x FROM u), 0)
-- case: TsqlExecuteStatement
EXEC sp_MSforeachdb 'USE [?]; SELECT DB_NAME()'
-- case: TsqlRaiserrorStatement
RAISERROR ('Error %d', 10, 1, @n) WITH LOG, NOWAIT
-- case: TsqlTryCatchStatement
BEGIN TRY SELECT 1/0 END TRY BEGIN CATCH THROW; END CATCH
-- case: TsqlSetVariableStatement
SET @msg = FORMATMESSAGE('x %s', @a)
-- case: SelectStatement
SELECT * FROM t1 FULL OUTER JOIN t2 ON t1.id = t2.id WHERE t1.id IS NULL OR t2.id IS NULL
-- case: SelectStatement
SELECT a, MAX(b) FROM t GROUP BY a HAVING COUNT(*) > 1 ORDER BY 2 DESC
-- case: SelectStatement
SELECT CAST(GETDATE() AS DATE) AS Today, CAST(GETDATE() AS TIME) AS Now, CONVERT(DATETIME2(0), '2020-01-01T00:00:00')
-- case: SelectStatement
SELECT TIMEFROMPARTS(1,2,3,0,0), DATETIME2FROMPARTS(2020,1,1,0,0,0,0,0), SWITCHOFFSET(SYSDATETIMEOFFSET(), '-08:00'), TODATETIMEOFFSET(d, '+01:00') FROM t
-- case: AlterTableStatement
ALTER TABLE t ADD CONSTRAINT PK PRIMARY KEY CLUSTERED (a ASC) WITH (ONLINE = ON)
-- case: AlterTableStatement
ALTER TABLE t DROP CONSTRAINT IF EXISTS PK, COLUMN IF EXISTS c
-- case: AlterTableStatement
ALTER TABLE t ALTER COLUMN c ADD MASKED WITH (FUNCTION = 'default()')
-- case: CreateIndexStatement
CREATE INDEX ix ON t (a) WITH (DROP_EXISTING = ON, ONLINE = ON, MAXDOP = 4, DATA_COMPRESSION = ROW)
-- case: TsqlAlterIndexStatement
ALTER INDEX ix ON t REORGANIZE WITH (LOB_COMPACTION = ON)
-- case: TsqlAlterIndexStatement
ALTER INDEX ix ON t DISABLE
-- case: TsqlDropIndexStatement
DROP INDEX IF EXISTS ix ON t
-- case: TsqlDropIndexStatement
DROP INDEX t.ix
-- case: TsqlCreateStatisticsStatement
CREATE STATISTICS st ON t (a, b) WHERE a > 0 WITH SAMPLE 50 PERCENT
-- case: SelectStatement
SELECT OBJECT_NAME(object_id), * FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED')
-- case: SelectStatement
SELECT t.*, v.x FROM t CROSS APPLY (VALUES (t.a), (t.b)) AS v(x)
-- case: SelectStatement
SELECT * FROM (SELECT ROW_NUMBER() OVER (PARTITION BY a ORDER BY b) rn, * FROM t) x WHERE rn = 1
-- case: SelectStatement
SELECT x.* INTO dbo.newtable FROM x
-- case: TsqlExecuteStatement
EXEC sp_executesql @stmt = @sql, @params = N'@id int', @id = @id
-- case: Batch(TsqlDeclareStatement,SelectStatement,TsqlPrintStatement)
DECLARE @sql nvarchar(max) = N'' SELECT @sql += QUOTENAME(name) + N',' FROM sys.columns PRINT @sql
-- case: TsqlInsertStatement
INSERT INTO dbo.Log (Msg) EXEC('SELECT 1')
-- case: Batch(TsqlDeclareStatement,TsqlExecuteStatement)
DECLARE @rc int EXEC @rc = sp_getapplock @Resource = 'x', @LockMode = 'Exclusive'
-- case: SelectStatement
SELECT 1 FROM t HAVING COUNT(*) > 0
-- case: SelectStatement
SELECT * FROM t WHERE a = @a OR @a IS NULL OPTION (RECOMPILE)
-- case: SelectStatement
SELECT * FROM OPENJSON(@j, '$.items') WITH (id int, name nvarchar(50) '$.n', tags nvarchar(max) AS JSON) AS j CROSS APPLY OPENJSON(j.tags) WITH (tag nvarchar(20) '$') t
-- case: TsqlCreateTriggerStatement
CREATE TRIGGER trg_ddl ON DATABASE FOR CREATE_TABLE, ALTER_TABLE AS BEGIN DECLARE @e xml = EVENTDATA() INSERT dbo.l (t) SELECT @e.value('(/EVENT_INSTANCE/ObjectName)[1]', 'nvarchar(256)') END
-- case: SelectStatement
SELECT a FROM t WHERE b IN (1, 2, 3) AND c NOT LIKE 'x%' AND d IS NOT NULL AND NOT EXISTS (SELECT 1 FROM u)
-- case: WithQuery
WITH a AS (SELECT 1 x), b AS (SELECT x FROM a) INSERT INTO t (x) SELECT x FROM b
-- case: WithQuery
WITH a AS (SELECT 1 x) UPDATE t SET x = a.x FROM t JOIN a ON 1 = 1
-- case: WithQuery
WITH a AS (SELECT 1 x) DELETE FROM t WHERE x IN (SELECT x FROM a)
-- case: WithQuery
WITH a AS (SELECT 1 x) MERGE t USING a ON t.x = a.x WHEN MATCHED THEN DELETE;
-- case: SelectStatement
SELECT * FROM t WHERE x = 1 GROUP BY ROLLUP (a, b), CUBE (c)
-- case: SelectStatement
SELECT * FROM t AS t1 JOIN t AS t2 ON t1.a = t2.a JOIN t AS t3 ON t2.a = t3.a LEFT JOIN (t AS t4 JOIN t AS t5 ON t4.a = t5.a) ON t3.a = t4.a
-- case: CreateViewStatement
CREATE VIEW dbo.v AS WITH c AS (SELECT 1 a) SELECT a FROM c
-- case: TsqlCreateFunctionStatement
CREATE FUNCTION dbo.f (@d date) RETURNS TABLE AS RETURN (WITH c AS (SELECT @d AS d) SELECT d FROM c)
-- case: TsqlCreateProcedureStatement
CREATE PROCEDURE dbo.p AS BEGIN SET NOCOUNT ON; WITH c AS (SELECT 1 a) SELECT a FROM c; END
-- case: TsqlDeclareStatement
DECLARE @x int = 1, @y varchar(10) = 'a', @z TABLE (a int)
-- case: Batch(SelectStatement,SelectStatement,TsqlDropObjectsStatement)
SELECT 1 AS a INTO #x; SELECT * FROM #x; DROP TABLE #x;
-- case: SelectStatement
SELECT CONCAT_WS(',', a, b), STRING_ESCAPE(a, 'json'), TRANSLATE(a, 'ab', 'cd'), SOUNDEX(a), DIFFERENCE(a, b) FROM t
-- case: SelectStatement
SELECT TRIM(LEADING '0' FROM a), LTRIM(a, '0'), RTRIM(a) FROM t
-- case: SelectStatement
SELECT IS_MEMBER('db_owner'), HAS_PERMS_BY_NAME('t', 'OBJECT', 'SELECT'), ORIGINAL_LOGIN(), SESSION_CONTEXT(N'k')
-- case: TsqlExecuteStatement
EXEC sp_set_session_context @key = N'k', @value = 1
-- case: SelectStatement
SELECT * FROM t1 WHERE EXISTS (SELECT 1 FROM t2 WHERE t2.a = t1.a INTERSECT SELECT 1 FROM t3 EXCEPT SELECT 2)
-- case: SelectStatement
SELECT * FROM t WHERE id = 1 AND [status] <> N'Closed' AND [type] = 'A' AND [level] > 2 AND [data] IS NOT NULL
-- case: SelectStatement
SELECT status, type, level, data, count, result, language, zone, first, last, source, target, action, path, domain, object FROM t
-- case: SelectStatement
SELECT name AS [Name], value AS [Value] FROM sys.configurations WHERE name LIKE '%max%'
