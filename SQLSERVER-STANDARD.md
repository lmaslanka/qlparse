# SQL Server (T-SQL) dialect coverage (2012–2025)

ISO core: SQL-STANDARD.md. This file tracks SQL Server-specific syntax beyond/diverging from ISO SQL, for use under the `SqlOptions.SqlServer` flag. Azure SQL Database / Managed Instance syntax is included where it matches the boxed product. `[ ]` = not yet implemented.

## Lexical

### Identifiers and quoting
- [x] Bracket-delimited identifiers (`[My Table]`) with `]]` escape
- [ ] Double-quoted identifiers under `QUOTED_IDENTIFIER ON` (default) vs. double-quoted strings under `OFF`
- [x] National character string literals (`N'...'`)
- [x] Binary literals (`0x0A1B`, empty `0x`)
- [x] Money literals with currency prefix (`$12.50`)
- [x] Local temporary object names (`#temp`) and global temporary names (`##temp`)
- [x] Local variables and parameters (`@name`) — reuse the existing `SqlOptions.AtParameters` lexing; `SqlServer` should imply it
- [x] System function tokens (`@@ROWCOUNT` `@@ERROR` …)
- [x] Unicode/extended identifier characters (`@`, `#`, `$`, `_` in regular identifiers)

### Comments and batches
- [x] Nested block comments (`/* outer /* inner */ still comment */`)
- [x] `GO [count]` batch separator (client-side; recognized only at the start of a line)
- [x] Optional statement terminators (statements separated by whitespace only, `;` required only before `WITH` CTEs, `MERGE`, `THROW`)

### Operators
- [x] Compound assignment operators (`+=` `-=` `*=` `/=` `%=` `&=` `|=` `^=`)
- [x] Non-ISO comparison operators (`!=` `!<` `!>`)
- [x] Modulo operator (`%`)
- [x] Bitwise operators (`&` `|` `^` `~`)
- [x] `+` as string concatenation operator
- [x] Scope-resolution operator for static type methods (`Type::Method()`)

## Data types
- [x] `VARCHAR(MAX)` `NVARCHAR(MAX)` `VARBINARY(MAX)`
- [x] `NCHAR` / `NVARCHAR` as first-class names
- [x] Legacy LOB types (`TEXT` `NTEXT` `IMAGE`)
- [x] `TINYINT`
- [x] `BIT`
- [x] `MONEY` / `SMALLMONEY`
- [x] `DATETIME` `SMALLDATETIME` `DATETIME2(n)` `DATETIMEOFFSET(n)`
- [x] `UNIQUEIDENTIFIER`
- [x] `ROWVERSION` / `TIMESTAMP` (T-SQL meaning: row version, not ISO timestamp)
- [x] `SQL_VARIANT`
- [x] `XML` and typed XML (`XML([CONTENT|DOCUMENT] schema_collection)`)
- [x] `HIERARCHYID`
- [x] Spatial types (`GEOMETRY` `GEOGRAPHY`)
- [x] `TABLE` type for table variables (`DECLARE @t TABLE (...)`)
- [x] `CURSOR` type for cursor variables and `OUTPUT` parameters
- [x] `VECTOR(n)` (2025)
- [x] Native `JSON` type (2025 / Azure SQL)
- [x] Alias types via `CREATE TYPE name FROM base_type [NULL|NOT NULL]`
- [x] User-defined table types via `CREATE TYPE name AS TABLE (...)`
- [x] CLR user-defined types via `CREATE TYPE name EXTERNAL NAME assembly.class`

## Operators and predicates
- [x] Legacy outer-join operators (`*=` `=*`; parse-only, removed in compatibility level 90+)
- [x] `IS [NOT] DISTINCT FROM` (2022)
- [x] `LIKE` character-class patterns (`[a-z]` `[^x]`; lexical only, inside the pattern string)
- [x] `CONTAINS(...)` / `FREETEXT(...)` full-text predicates
- [x] `MATCH (node-edge->node ...)` graph predicate (see Graph tables)

## Functions

### String
- [x] `LEN()` / `DATALENGTH()`
- [x] `CHARINDEX()` / `PATINDEX()`
- [x] `STUFF()` `REPLICATE()` `REVERSE()` `SPACE()`
- [x] `STRING_SPLIT(str, sep [, enable_ordinal])`
- [x] `STRING_AGG(expr, sep) [WITHIN GROUP (ORDER BY ...)]`
- [x] `CONCAT()` / `CONCAT_WS()`
- [x] `TRANSLATE()`
- [x] `TRIM([LEADING|TRAILING|BOTH] [chars FROM] str)` / `LTRIM(str, chars)` / `RTRIM(str, chars)` (2022 forms)
- [x] `FORMAT(value, format [, culture])`
- [x] `QUOTENAME()`
- [x] `LEFT()` `RIGHT()` `SUBSTRING()` without `FROM`/`FOR` keywords
- [x] `SOUNDEX()` / `DIFFERENCE()`
- [x] `STRING_ESCAPE()`
- [x] `REGEXP_LIKE()` `REGEXP_REPLACE()` `REGEXP_SUBSTR()` `REGEXP_INSTR()` `REGEXP_COUNT()` `REGEXP_MATCHES()` `REGEXP_SPLIT_TO_TABLE()` (2025)

### Conversion
- [x] `CONVERT(data_type[(length)], expr [, style])`
- [x] `TRY_CONVERT()` / `TRY_CAST()`
- [x] `PARSE(str AS type [USING culture])` / `TRY_PARSE()`

### Date / time
- [x] `GETDATE()` `GETUTCDATE()` `SYSDATETIME()` `SYSUTCDATETIME()` `SYSDATETIMEOFFSET()`
- [x] `CURRENT_TIMESTAMP` without parentheses as `DATETIME`
- [x] `DATEADD()` `DATEDIFF()` `DATEDIFF_BIG()` `DATEPART()` `DATENAME()` with bare datepart keywords (`year`, `yy`, `mm`, `dw`, …)
- [x] `EOMONTH()`
- [x] `DATEFROMPARTS()` `DATETIME2FROMPARTS()` `DATETIMEOFFSETFROMPARTS()` `TIMEFROMPARTS()` `SMALLDATETIMEFROMPARTS()`
- [x] `SWITCHOFFSET()` / `TODATETIMEOFFSET()`
- [x] `AT TIME ZONE 'zone'` expression suffix
- [x] `DATETRUNC()` / `DATE_BUCKET()` (2022)
- [x] `ISDATE()`

### Logical and null handling
- [x] `IIF(cond, a, b)`
- [x] `CHOOSE(index, v1, v2, ...)`
- [x] `ISNULL(expr, replacement)`
- [x] `GREATEST()` / `LEAST()` (2022)

### Set-returning / table functions
- [x] `GENERATE_SERIES(start, stop [, step])` (2022)
- [x] Inline and multi-statement table-valued function calls in `FROM`
- [x] `OPENJSON(json [, path]) [WITH (col type 'path' [AS JSON], ...)]`
- [x] `OPENXML(handle, xpath [, flags]) [WITH (...)]`
- [x] `OPENROWSET(...)` (provider, `BULK`, and format-file forms)
- [x] `OPENQUERY(linked_server, 'query')`
- [x] `OPENDATASOURCE(provider, init_string).db.schema.obj`
- [x] `CONTAINSTABLE()` / `FREETEXTTABLE()`

### JSON
- [x] `JSON_VALUE()` `JSON_QUERY()` `JSON_MODIFY()` `ISJSON()`
- [x] `JSON_PATH_EXISTS()`
- [x] `JSON_OBJECT(key : value, ... [NULL ON NULL|ABSENT ON NULL])` / `JSON_ARRAY()` (2022 colon syntax)
- [x] `JSON_OBJECTAGG()` / `JSON_ARRAYAGG()` (2025)
- [x] `JSON_CONTAINS()` (2025)

### XML
- [x] XML data type methods (`.value()` `.query()` `.nodes()` `.exist()` `.modify()`)
- [x] `WITH XMLNAMESPACES (...)` prefix to `SELECT`/DML

### Aggregates and analytics
- [x] `COUNT_BIG()`
- [x] `CHECKSUM_AGG()` / `CHECKSUM()` / `BINARY_CHECKSUM()`
- [x] `STDEV()` `STDEVP()` `VAR()` `VARP()`
- [x] `APPROX_COUNT_DISTINCT()` / `APPROX_PERCENTILE_CONT()` / `APPROX_PERCENTILE_DISC()`
- [x] `GROUPING_ID()`
- [x] `PERCENTILE_CONT()` / `PERCENTILE_DISC()` with `WITHIN GROUP (...) OVER (PARTITION BY ...)`

### System and introspection
- [x] `@@ROWCOUNT` `@@ERROR` `@@IDENTITY` `@@TRANCOUNT` `@@SPID` `@@VERSION` `@@SERVERNAME` `@@FETCH_STATUS`
- [x] `ROWCOUNT_BIG()` `SCOPE_IDENTITY()` `IDENT_CURRENT()`
- [x] `ERROR_NUMBER()` `ERROR_MESSAGE()` `ERROR_SEVERITY()` `ERROR_STATE()` `ERROR_LINE()` `ERROR_PROCEDURE()`
- [x] `OBJECT_ID()` `OBJECT_NAME()` `DB_ID()` `DB_NAME()` `SCHEMA_NAME()` `COL_LENGTH()`
- [x] `NEWID()` / `NEWSEQUENTIALID()`
- [x] `SESSION_CONTEXT()` / `CONTEXT_INFO()`
- [x] `USER_NAME()` `SUSER_SNAME()` `ORIGINAL_LOGIN()`

### Vector (2025)
- [x] `VECTOR_DISTANCE()` `VECTOR_NORM()` `VECTOR_NORMALIZE()`
- [x] `VECTOR_SEARCH()` table-valued function
- [x] `AI_GENERATE_EMBEDDINGS(... USE MODEL model)` / `AI_GENERATE_CHUNKS()`

### CLR and type methods
- [x] Instance method calls on UDT/spatial/hierarchyid values (`geo.STDistance(@p)`, `node.GetAncestor(1)`)
- [x] Static method calls (`geography::Point(lat, lon, 4326)`, `hierarchyid::GetRoot()`)
- [x] Property access on UDT values (`@p.X`, `@p.STSrid`)

## Query expressions
- [x] `SELECT TOP (n) [PERCENT] [WITH TIES]` (and legacy `TOP n` without parentheses)
- [x] Variable assignment in select lists (`SELECT @v = expr, @w = expr FROM ...`)
- [x] `SELECT ... INTO new_table [ON filegroup] FROM ...`
- [x] `alias = expr` select-list alias form and string-literal aliases (`expr AS 'alias'`)
- [x] Table hints (`FROM t WITH (NOLOCK, READPAST, UPDLOCK, HOLDLOCK, INDEX(ix), FORCESEEK, ...)`)
- [x] `OPTION (RECOMPILE, MAXDOP n, OPTIMIZE FOR (...), USE HINT ('...'), LABEL = '...', ...)` query hints
- [x] Join hints (`INNER HASH JOIN`, `LEFT MERGE JOIN`, `LOOP`, `REMOTE`)
- [x] `CROSS APPLY` / `OUTER APPLY`
- [x] `PIVOT (agg(col) FOR col IN ([a], [b])) AS p`
- [x] `UNPIVOT (val FOR col IN ([a], [b])) AS u`
- [x] `FOR XML {RAW [('el')]|AUTO|EXPLICIT|PATH [('el')]} [, ROOT('r')] [, TYPE] [, ELEMENTS [XSINIL|ABSENT]] [, XMLSCHEMA]`
- [x] `FOR JSON {AUTO|PATH} [, ROOT('r')] [, INCLUDE_NULL_VALUES] [, WITHOUT_ARRAY_WRAPPER]`
- [x] `FOR BROWSE`
- [x] Temporal queries (`FOR SYSTEM_TIME {AS OF|FROM ... TO|BETWEEN ... AND|CONTAINED IN (...)|ALL}`)
- [x] `OFFSET n ROWS FETCH {FIRST|NEXT} n ROWS ONLY` as the only `FETCH` form (no `WITH TIES`/`PERCENT` here)
- [x] `WINDOW` clause and named windows (2022)
- [x] Multi-part names (`server.database.schema.object`, `database..object`, `.schema.object`)
- [x] Derived table column aliases without `AS` and `VALUES` row constructors as derived tables
- [x] `TABLESAMPLE [SYSTEM] (n {PERCENT|ROWS}) [REPEATABLE (seed)]`
- [x] `GROUP BY ALL` (deprecated) and `GROUP BY ... WITH ROLLUP|CUBE`
- [x] `COMPUTE` / `COMPUTE BY` (legacy, parse-only)

## DML
- [x] `OUTPUT inserted.*|deleted.* [INTO {@table|table} [(cols)]]` on `INSERT` / `UPDATE` / `DELETE` / `MERGE`
- [x] `$action` in `MERGE ... OUTPUT`
- [x] `INSERT TOP (n)` / `UPDATE TOP (n)` / `DELETE TOP (n)`
- [x] `UPDATE t SET ... FROM t JOIN ...`
- [x] `DELETE [FROM] t FROM t JOIN ...` (second `FROM`)
- [x] `UPDATE ... SET @v = col = expr` combined variable/column assignment
- [x] `UPDATE ... SET col.WRITE(expr, offset, length)` partial LOB updates
- [x] `UPDATE ... SET udt_col.Method(...)` mutator calls
- [x] `INSERT INTO t EXEC[UTE] proc ...` / `INSERT ... EXEC ('sql')`
- [x] `INSERT` without `INTO`
- [x] `MERGE ... WHEN NOT MATCHED [BY TARGET]` / `WHEN NOT MATCHED BY SOURCE` with extra `AND` conditions
- [x] Table hints on DML targets (`UPDATE t WITH (ROWLOCK)`, `MERGE t WITH (HOLDLOCK)`)
- [x] `WHERE CURRENT OF [GLOBAL] cursor` with T-SQL cursor names and variables
- [x] `BULK INSERT table FROM 'file' WITH (FORMAT = 'CSV', FIELDTERMINATOR = ..., ...)`
- [x] `TRUNCATE TABLE ... WITH (PARTITIONS (1, 3 TO 5))`

## Schema / DDL

### Tables
- [x] `IDENTITY [(seed, increment)]` column property
- [x] Computed columns (`col AS expr [PERSISTED [NOT NULL]]`)
- [x] `ON {filegroup|partition_scheme(col)|"default"}` / `TEXTIMAGE_ON` / `FILESTREAM_ON`
- [x] Column options `FILESTREAM` `SPARSE` `ROWGUIDCOL` `COLUMN_SET FOR ALL_SPARSE_COLUMNS` `COLLATE`
- [x] `WITH (DATA_COMPRESSION = {NONE|ROW|PAGE|COLUMNSTORE} [ON PARTITIONS (...)], ...)`
- [x] System-versioned temporal tables (`PERIOD FOR SYSTEM_TIME (start, end)`, `GENERATED ALWAYS AS ROW START|END [HIDDEN]`, `WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = ..., HISTORY_RETENTION_PERIOD = ...))`)
- [x] Memory-optimized tables (`WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA)`)
- [x] Ledger tables (`WITH (LEDGER = ON [(APPEND_ONLY = ON)])`)
- [x] Graph tables (`CREATE TABLE ... AS NODE` / `AS EDGE`)
- [x] `CREATE TABLE #temp` / `##temp` and table variables in `DECLARE`
- [x] `CREATE OR ALTER` for views, procedures, functions, triggers
- [x] `DROP {TABLE|VIEW|PROCEDURE|...} IF EXISTS a, b, c`
- [x] `ALTER TABLE ... ALTER COLUMN col type [NULL|NOT NULL] [WITH (ONLINE = ON)]`
- [x] `ALTER TABLE ... ADD col ...` without `COLUMN` keyword, multiple comma-separated adds
- [x] `ALTER TABLE ... DROP [COLUMN|CONSTRAINT] a, b`
- [x] `ALTER TABLE ... [WITH {CHECK|NOCHECK}] {CHECK|NOCHECK} CONSTRAINT {ALL|name}`
- [x] `ALTER TABLE ... {ENABLE|DISABLE} TRIGGER {ALL|name}`
- [x] `ALTER TABLE ... SWITCH [PARTITION n] TO target [PARTITION m]`
- [x] `ALTER TABLE ... SET (LOCK_ESCALATION = ..., SYSTEM_VERSIONING = ...)`
- [x] `ALTER TABLE ... REBUILD [PARTITION = ...] WITH (...)`
- [x] `sp_rename` style renames via `EXEC` (no dedicated `RENAME` syntax)

### Partitioning
- [x] `CREATE PARTITION FUNCTION fn (type) AS RANGE {LEFT|RIGHT} FOR VALUES (...)`
- [x] `CREATE PARTITION SCHEME ps AS PARTITION fn [ALL] TO (filegroups)`
- [x] `ALTER PARTITION FUNCTION ... {SPLIT|MERGE} RANGE (value)`
- [x] `ALTER PARTITION SCHEME ... NEXT USED filegroup`
- [x] `$PARTITION.fn(expr)`

### Indexes
- [x] `CREATE [UNIQUE] {CLUSTERED|NONCLUSTERED} INDEX`
- [x] `CREATE [CLUSTERED|NONCLUSTERED] COLUMNSTORE INDEX` (with `ORDER (...)`)
- [x] Filtered indexes (`CREATE INDEX ... WHERE predicate`)
- [x] `INCLUDE (...)`
- [x] `WITH (FILLFACTOR, PAD_INDEX, ONLINE, SORT_IN_TEMPDB, DROP_EXISTING, RESUMABLE, MAXDOP, ...)`
- [x] `CREATE [PRIMARY] XML INDEX ... [USING XML INDEX ... FOR {VALUE|PATH|PROPERTY}]`
- [x] `CREATE SPATIAL INDEX ... USING {GEOMETRY_GRID|GEOGRAPHY_GRID|...}`
- [x] `CREATE JSON INDEX` / `CREATE VECTOR INDEX` (2025)
- [x] Memory-optimized hash indexes (`INDEX ix HASH (col) WITH (BUCKET_COUNT = n)`)
- [x] Inline index definitions inside `CREATE TABLE`
- [x] `ALTER INDEX {name|ALL} ON t {REBUILD|REORGANIZE|DISABLE|RESUME|PAUSE|ABORT|SET (...)}`
- [x] `DROP INDEX ix ON t` / legacy `DROP INDEX t.ix`
- [x] `CREATE STATISTICS` / `UPDATE STATISTICS ... WITH FULLSCAN` / `DROP STATISTICS`

### Views
- [x] `WITH SCHEMABINDING` / `ENCRYPTION` / `VIEW_METADATA`
- [x] Indexed views (`CREATE UNIQUE CLUSTERED INDEX` on a schema-bound view)

### Sequences and synonyms
- [x] `NEXT VALUE FOR seq [OVER (ORDER BY ...)]`
- [x] `sp_sequence_get_range` via `EXEC`
- [x] `CREATE SYNONYM name FOR [server.][db.][schema.]object`

### Databases, schemas, and storage
- [x] `CREATE DATABASE ... ON [PRIMARY] (NAME = ..., FILENAME = ..., SIZE = ...) LOG ON (...) [COLLATE ...] [WITH ...]`
- [x] `CREATE DATABASE ... AS SNAPSHOT OF` / `FOR ATTACH`
- [x] `ALTER DATABASE ... {SET option|ADD FILE|ADD FILEGROUP|MODIFY FILE|MODIFY NAME}`
- [x] `ALTER DATABASE SCOPED CONFIGURATION SET ...`
- [x] `CREATE SCHEMA name [AUTHORIZATION owner] [schema_element ...]`
- [x] `ALTER SCHEMA target TRANSFER [OBJECT::]source`
- [x] `ALTER AUTHORIZATION ON class::object TO principal`

### Other objects
- [x] `CREATE XML SCHEMA COLLECTION`
- [x] `CREATE ASSEMBLY ... FROM ... WITH PERMISSION_SET = ...`
- [x] CLR modules (`CREATE PROCEDURE|FUNCTION|TRIGGER|AGGREGATE ... EXTERNAL NAME a.c.m`)
- [x] Legacy `CREATE DEFAULT` / `CREATE RULE` and `sp_bindefault` / `sp_bindrule`
- [x] PolyBase (`CREATE EXTERNAL DATA SOURCE`, `CREATE EXTERNAL FILE FORMAT`, `CREATE EXTERNAL TABLE`, `CREATE EXTERNAL TABLE AS SELECT`)
- [x] `CREATE EXTERNAL MODEL` (2025)

### Graph tables
- [x] `AS NODE` / `AS EDGE` table declarations and `$node_id` `$edge_id` `$from_id` `$to_id` pseudo-columns
- [x] `MATCH` patterns (`a-(e)->b`, `a<-(e)-b`, `SHORTEST_PATH(...)` with `+` / `{1,n}` quantifiers)
- [x] Graph aggregate helpers (`LAST_NODE()`, `STRING_AGG(...) WITHIN GROUP (GRAPH PATH)`)

## Constraints
- [x] `PRIMARY KEY` / `UNIQUE` with `CLUSTERED|NONCLUSTERED` and `WITH (...)` index options
- [x] Named inline `DEFAULT` constraints (`col int CONSTRAINT df_x DEFAULT 0`)
- [x] `ALTER TABLE ... ADD [CONSTRAINT name] DEFAULT expr FOR col`
- [x] `NOT FOR REPLICATION`
- [x] `ALTER TABLE ... WITH NOCHECK ADD CONSTRAINT ...`
- [x] Edge constraints (`CONSTRAINT ec CONNECTION (Node1 TO Node2) ON DELETE CASCADE`)

## Triggers
- [x] `CREATE [OR ALTER] TRIGGER ... ON t [WITH ...] {FOR|AFTER|INSTEAD OF} {INSERT|UPDATE|DELETE}[, ...] [NOT FOR REPLICATION] AS ...`
- [x] `inserted` / `deleted` pseudo-tables
- [x] `UPDATE(col)` / `COLUMNS_UPDATED()` inside triggers
- [x] DDL triggers (`ON {DATABASE|ALL SERVER} FOR {CREATE_TABLE|DDL_DATABASE_LEVEL_EVENTS|...}`) and `EVENTDATA()`
- [x] Logon triggers (`ON ALL SERVER FOR LOGON`)
- [x] `{ENABLE|DISABLE} TRIGGER {name|ALL} ON {object|DATABASE|ALL SERVER}`
- [x] Event notifications (`CREATE EVENT NOTIFICATION ... TO SERVICE ...`)

## Procedural T-SQL
- [x] `DECLARE @a type [= expr], @b type, @t TABLE (...), @c CURSOR`
- [x] `SET @v = expr` and compound `SET @v += expr`
- [x] `BEGIN ... END` statement blocks
- [x] `IF cond stmt [ELSE stmt]` (single statement or block, no `THEN`/`END IF`)
- [x] `WHILE cond stmt` / `BREAK` / `CONTINUE`
- [x] `GOTO label` / `label:`
- [x] `RETURN [expr]`
- [x] `WAITFOR {DELAY 'hh:mm:ss'|TIME 'hh:mm:ss'|(RECEIVE ...)} [, TIMEOUT n]`
- [x] `BEGIN TRY ... END TRY BEGIN CATCH ... END CATCH`
- [x] `THROW [number, message, state]`
- [x] `RAISERROR (msg, severity, state [, args]) [WITH LOG|NOWAIT|SETERROR]`
- [x] `PRINT expr`
- [x] `EXEC[UTE] [@ret =] proc [@p =] value [OUTPUT] [DEFAULT], ... [WITH RECOMPILE|RESULT SETS (...)]`
- [x] `EXEC[UTE] ('sql' + @x) [AS {LOGIN|USER} = '...'] [AT linked_server]`
- [x] `EXEC sp_executesql @stmt, N'@p int', @p = 1`
- [x] Procedure numbers (`proc;2`) and module-name variables (`EXEC @procname`)
- [x] `CREATE [OR ALTER] PROC[EDURE] name [@p type [VARYING] [= default] [OUT|OUTPUT] [READONLY], ...] [WITH {RECOMPILE|ENCRYPTION|EXECUTE AS ...}] [FOR REPLICATION] AS ...`
- [x] Scalar functions (`CREATE FUNCTION ... RETURNS type [WITH {SCHEMABINDING|RETURNS NULL ON NULL INPUT|INLINE = ON}] AS BEGIN ... RETURN expr END`)
- [x] Inline table-valued functions (`RETURNS TABLE AS RETURN (select)`)
- [x] Multi-statement table-valued functions (`RETURNS @t TABLE (...) AS BEGIN ... RETURN END`)
- [x] Natively compiled modules (`WITH NATIVE_COMPILATION, SCHEMABINDING AS BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'...') ... END`)
- [x] T-SQL cursor declarations (`DECLARE c CURSOR [LOCAL|GLOBAL] [FORWARD_ONLY|SCROLL] [STATIC|KEYSET|DYNAMIC|FAST_FORWARD] [READ_ONLY|SCROLL_LOCKS|OPTIMISTIC] FOR select [FOR UPDATE [OF ...]]`)
- [x] `OPEN` / `FETCH [NEXT|PRIOR|FIRST|LAST|ABSOLUTE n|RELATIVE n] FROM [GLOBAL] c INTO @a, @b` / `CLOSE` / `DEALLOCATE`
- [x] `SET @cursor_var = CURSOR FOR ...`

## Security
- [x] `CREATE LOGIN name {WITH PASSWORD = '...' [HASHED] [MUST_CHANGE], ...|FROM {WINDOWS|EXTERNAL PROVIDER|CERTIFICATE c|ASYMMETRIC KEY k}}`
- [x] `CREATE USER name [{FOR|FROM} LOGIN l|WITHOUT LOGIN|FROM EXTERNAL PROVIDER] [WITH DEFAULT_SCHEMA = s]`
- [x] `CREATE ROLE` / `CREATE SERVER ROLE` / `CREATE APPLICATION ROLE`
- [x] `ALTER [SERVER] ROLE r {ADD|DROP} MEMBER m`
- [x] `GRANT|DENY|REVOKE perm ON {OBJECT|SCHEMA|DATABASE|TYPE|XML SCHEMA COLLECTION|...}::name TO principal [WITH GRANT OPTION] [AS principal]`
- [x] `DENY` statement
- [x] `EXECUTE AS {CALLER|SELF|OWNER|USER = '...'|LOGIN = '...'}` / `REVERT [WITH COOKIE = @c]`
- [x] Row-level security (`CREATE SECURITY POLICY ... ADD {FILTER|BLOCK} PREDICATE fn(col) ON t [AFTER INSERT|...] WITH (STATE = ON)`)
- [x] Dynamic data masking (`col ... MASKED WITH (FUNCTION = 'partial(1,"xx",1)')`, `ALTER COLUMN ... ADD|DROP MASKED`)
- [x] Always Encrypted (`ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = k, ENCRYPTION_TYPE = ..., ALGORITHM = '...')`, `CREATE COLUMN MASTER KEY` / `COLUMN ENCRYPTION KEY`)
- [x] Keys and certificates (`CREATE MASTER KEY`, `CREATE CERTIFICATE`, `CREATE SYMMETRIC|ASYMMETRIC KEY`, `OPEN|CLOSE SYMMETRIC KEY`, `ADD SIGNATURE`)
- [x] Credentials and auditing (`CREATE [DATABASE SCOPED] CREDENTIAL`, `CREATE SERVER AUDIT`, `CREATE DATABASE AUDIT SPECIFICATION`)

## Transactions and session
- [x] `BEGIN TRAN[SACTION] [name|@var] [WITH MARK ['desc']]`
- [x] `SAVE TRAN[SACTION] name`
- [x] `COMMIT [TRAN[SACTION]] [name] [WITH (DELAYED_DURABILITY = ON)]` / `ROLLBACK [TRAN[SACTION]] [name]`
- [x] `COMMIT WORK` / `ROLLBACK WORK`
- [x] `BEGIN DISTRIBUTED TRAN[SACTION]`
- [x] `SET` session options (`NOCOUNT` `XACT_ABORT` `ANSI_NULLS` `ANSI_PADDING` `ARITHABORT` `CONCAT_NULL_YIELDS_NULL` `DATEFIRST` `DATEFORMAT` `LANGUAGE` `LOCK_TIMEOUT` `DEADLOCK_PRIORITY` `ROWCOUNT` `TEXTSIZE` `IMPLICIT_TRANSACTIONS` `CONTEXT_INFO` `{ON|OFF}`)
- [x] `SET TRANSACTION ISOLATION LEVEL {READ UNCOMMITTED|READ COMMITTED|REPEATABLE READ|SNAPSHOT|SERIALIZABLE}`
- [x] `SET IDENTITY_INSERT t {ON|OFF}`
- [x] Diagnostics options (`SET STATISTICS {IO|TIME|XML|PROFILE} ON`, `SET SHOWPLAN_{TEXT|ALL|XML} ON`, `SET NOEXEC ON`, `SET PARSEONLY ON`)
- [x] `USE database`

## Administration and utility commands
- [x] `DBCC command [(args)] [WITH options]` (`CHECKDB`, `SHRINKFILE`, `FREEPROCCACHE`, `INPUTBUFFER`, …)
- [x] `BACKUP {DATABASE|LOG} name TO {DISK|URL} = '...' [WITH ...]`
- [x] `RESTORE {DATABASE|LOG|HEADERONLY|FILELISTONLY|VERIFYONLY} ... FROM ... [WITH MOVE ..., NORECOVERY, ...]`
- [x] `CHECKPOINT [duration]`
- [x] `KILL {session_id|UOW} [WITH STATUSONLY]`
- [x] `RECONFIGURE [WITH OVERRIDE]`
- [x] `SHUTDOWN [WITH NOWAIT]`
- [x] `ALTER SERVER CONFIGURATION SET ...`
- [x] Availability groups and endpoints (`CREATE|ALTER AVAILABILITY GROUP`, `CREATE ENDPOINT`)
- [x] Resource Governor (`CREATE RESOURCE POOL`, `CREATE WORKLOAD GROUP`, `ALTER RESOURCE GOVERNOR`)
- [x] Extended events (`CREATE EVENT SESSION ... ON SERVER ADD EVENT ... ADD TARGET ...`)
- [x] Legacy text/image statements (`READTEXT` `WRITETEXT` `UPDATETEXT`)
- [x] `SETUSER` (legacy)

## Service Broker
- [x] `CREATE MESSAGE TYPE` / `CREATE CONTRACT` / `CREATE QUEUE` / `CREATE SERVICE` / `CREATE ROUTE`
- [x] `BEGIN DIALOG [CONVERSATION] @h FROM SERVICE s TO SERVICE 't' ON CONTRACT c [WITH ...]`
- [x] `SEND ON CONVERSATION @h [MESSAGE TYPE m] (body)`
- [x] `RECEIVE [TOP (n)] cols FROM queue [INTO @t] [WHERE ...]`
- [x] `END CONVERSATION @h [WITH ERROR = n DESCRIPTION = '...' | WITH CLEANUP]`
- [x] `GET CONVERSATION GROUP @g FROM queue`
- [x] `MOVE CONVERSATION`

## Full text search
- [x] `CONTAINS(col|(cols)|*, 'search condition' [, LANGUAGE l])`
- [x] `FREETEXT(col|(cols)|*, 'text' [, LANGUAGE l])`
- [x] `CONTAINSTABLE(t, col, '...' [, top_n])` / `FREETEXTTABLE(...)` with `KEY` / `RANK` columns
- [x] `CREATE FULLTEXT CATALOG` / `ALTER FULLTEXT CATALOG`
- [x] `CREATE FULLTEXT INDEX ON t (col [TYPE COLUMN c] [LANGUAGE l]) KEY INDEX ix [ON catalog] [WITH ...]`
- [x] `CREATE FULLTEXT STOPLIST` / `CREATE SEARCH PROPERTY LIST`
- [x] `SEMANTICKEYPHRASETABLE()` / `SEMANTICSIMILARITYTABLE()` / `SEMANTICSIMILARITYDETAILSTABLE()`

## System objects and pseudo-columns
- [x] `sys.*` catalog views, `INFORMATION_SCHEMA`, and `dm_*` DMVs as ordinary multi-part names
- [x] System stored procedures (`sp_*`, `xp_*`) called via `EXEC` without schema qualification
- [x] `$IDENTITY` / `$ROWGUID` pseudo-columns
- [x] `$PARTITION.fn(expr)`
- [x] Graph pseudo-columns (`$node_id` `$edge_id` `$from_id` `$to_id`)
