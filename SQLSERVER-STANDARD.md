# SQL Server (T-SQL) dialect coverage (2012–2025)

ISO core: SQL-STANDARD.md. This file tracks SQL Server-specific syntax beyond/diverging from ISO SQL, for use under the `SqlOptions.SqlServer` flag. Azure SQL Database / Managed Instance syntax is included where it matches the boxed product. `[ ]` = not yet implemented.

## Lexical

### Identifiers and quoting
- [ ] Bracket-delimited identifiers (`[My Table]`) with `]]` escape
- [ ] Double-quoted identifiers under `QUOTED_IDENTIFIER ON` (default) vs. double-quoted strings under `OFF`
- [ ] National character string literals (`N'...'`)
- [ ] Binary literals (`0x0A1B`, empty `0x`)
- [ ] Money literals with currency prefix (`$12.50`)
- [ ] Local temporary object names (`#temp`) and global temporary names (`##temp`)
- [ ] Local variables and parameters (`@name`) — reuse the existing `SqlOptions.AtParameters` lexing; `SqlServer` should imply it
- [ ] System function tokens (`@@ROWCOUNT` `@@ERROR` …)
- [ ] Unicode/extended identifier characters (`@`, `#`, `$`, `_` in regular identifiers)

### Comments and batches
- [ ] Nested block comments (`/* outer /* inner */ still comment */`)
- [ ] `GO [count]` batch separator (client-side; recognized only at the start of a line)
- [ ] Optional statement terminators (statements separated by whitespace only, `;` required only before `WITH` CTEs, `MERGE`, `THROW`)

### Operators
- [ ] Compound assignment operators (`+=` `-=` `*=` `/=` `%=` `&=` `|=` `^=`)
- [ ] Non-ISO comparison operators (`!=` `!<` `!>`)
- [ ] Modulo operator (`%`)
- [ ] Bitwise operators (`&` `|` `^` `~`)
- [ ] `+` as string concatenation operator
- [ ] Scope-resolution operator for static type methods (`Type::Method()`)

## Data types
- [ ] `VARCHAR(MAX)` `NVARCHAR(MAX)` `VARBINARY(MAX)`
- [ ] `NCHAR` / `NVARCHAR` as first-class names
- [ ] Legacy LOB types (`TEXT` `NTEXT` `IMAGE`)
- [ ] `TINYINT`
- [ ] `BIT`
- [ ] `MONEY` / `SMALLMONEY`
- [ ] `DATETIME` `SMALLDATETIME` `DATETIME2(n)` `DATETIMEOFFSET(n)`
- [ ] `UNIQUEIDENTIFIER`
- [ ] `ROWVERSION` / `TIMESTAMP` (T-SQL meaning: row version, not ISO timestamp)
- [ ] `SQL_VARIANT`
- [ ] `XML` and typed XML (`XML([CONTENT|DOCUMENT] schema_collection)`)
- [ ] `HIERARCHYID`
- [ ] Spatial types (`GEOMETRY` `GEOGRAPHY`)
- [ ] `TABLE` type for table variables (`DECLARE @t TABLE (...)`)
- [ ] `CURSOR` type for cursor variables and `OUTPUT` parameters
- [ ] `VECTOR(n)` (2025)
- [ ] Native `JSON` type (2025 / Azure SQL)
- [ ] Alias types via `CREATE TYPE name FROM base_type [NULL|NOT NULL]`
- [ ] User-defined table types via `CREATE TYPE name AS TABLE (...)`
- [ ] CLR user-defined types via `CREATE TYPE name EXTERNAL NAME assembly.class`

## Operators and predicates
- [ ] Legacy outer-join operators (`*=` `=*`; parse-only, removed in compatibility level 90+)
- [ ] `IS [NOT] DISTINCT FROM` (2022)
- [ ] `LIKE` character-class patterns (`[a-z]` `[^x]`; lexical only, inside the pattern string)
- [ ] `CONTAINS(...)` / `FREETEXT(...)` full-text predicates
- [ ] `MATCH (node-edge->node ...)` graph predicate (see Graph tables)

## Functions

### String
- [ ] `LEN()` / `DATALENGTH()`
- [ ] `CHARINDEX()` / `PATINDEX()`
- [ ] `STUFF()` `REPLICATE()` `REVERSE()` `SPACE()`
- [ ] `STRING_SPLIT(str, sep [, enable_ordinal])`
- [ ] `STRING_AGG(expr, sep) [WITHIN GROUP (ORDER BY ...)]`
- [ ] `CONCAT()` / `CONCAT_WS()`
- [ ] `TRANSLATE()`
- [ ] `TRIM([LEADING|TRAILING|BOTH] [chars FROM] str)` / `LTRIM(str, chars)` / `RTRIM(str, chars)` (2022 forms)
- [ ] `FORMAT(value, format [, culture])`
- [ ] `QUOTENAME()`
- [ ] `LEFT()` `RIGHT()` `SUBSTRING()` without `FROM`/`FOR` keywords
- [ ] `SOUNDEX()` / `DIFFERENCE()`
- [ ] `STRING_ESCAPE()`
- [ ] `REGEXP_LIKE()` `REGEXP_REPLACE()` `REGEXP_SUBSTR()` `REGEXP_INSTR()` `REGEXP_COUNT()` `REGEXP_MATCHES()` `REGEXP_SPLIT_TO_TABLE()` (2025)

### Conversion
- [ ] `CONVERT(data_type[(length)], expr [, style])`
- [ ] `TRY_CONVERT()` / `TRY_CAST()`
- [ ] `PARSE(str AS type [USING culture])` / `TRY_PARSE()`

### Date / time
- [ ] `GETDATE()` `GETUTCDATE()` `SYSDATETIME()` `SYSUTCDATETIME()` `SYSDATETIMEOFFSET()`
- [ ] `CURRENT_TIMESTAMP` without parentheses as `DATETIME`
- [ ] `DATEADD()` `DATEDIFF()` `DATEDIFF_BIG()` `DATEPART()` `DATENAME()` with bare datepart keywords (`year`, `yy`, `mm`, `dw`, …)
- [ ] `EOMONTH()`
- [ ] `DATEFROMPARTS()` `DATETIME2FROMPARTS()` `DATETIMEOFFSETFROMPARTS()` `TIMEFROMPARTS()` `SMALLDATETIMEFROMPARTS()`
- [ ] `SWITCHOFFSET()` / `TODATETIMEOFFSET()`
- [ ] `AT TIME ZONE 'zone'` expression suffix
- [ ] `DATETRUNC()` / `DATE_BUCKET()` (2022)
- [ ] `ISDATE()`

### Logical and null handling
- [ ] `IIF(cond, a, b)`
- [ ] `CHOOSE(index, v1, v2, ...)`
- [ ] `ISNULL(expr, replacement)`
- [ ] `GREATEST()` / `LEAST()` (2022)

### Set-returning / table functions
- [ ] `GENERATE_SERIES(start, stop [, step])` (2022)
- [ ] Inline and multi-statement table-valued function calls in `FROM`
- [ ] `OPENJSON(json [, path]) [WITH (col type 'path' [AS JSON], ...)]`
- [ ] `OPENXML(handle, xpath [, flags]) [WITH (...)]`
- [ ] `OPENROWSET(...)` (provider, `BULK`, and format-file forms)
- [ ] `OPENQUERY(linked_server, 'query')`
- [ ] `OPENDATASOURCE(provider, init_string).db.schema.obj`
- [ ] `CONTAINSTABLE()` / `FREETEXTTABLE()`

### JSON
- [ ] `JSON_VALUE()` `JSON_QUERY()` `JSON_MODIFY()` `ISJSON()`
- [ ] `JSON_PATH_EXISTS()`
- [ ] `JSON_OBJECT(key : value, ... [NULL ON NULL|ABSENT ON NULL])` / `JSON_ARRAY()` (2022 colon syntax)
- [ ] `JSON_OBJECTAGG()` / `JSON_ARRAYAGG()` (2025)
- [ ] `JSON_CONTAINS()` (2025)

### XML
- [ ] XML data type methods (`.value()` `.query()` `.nodes()` `.exist()` `.modify()`)
- [ ] `WITH XMLNAMESPACES (...)` prefix to `SELECT`/DML

### Aggregates and analytics
- [ ] `COUNT_BIG()`
- [ ] `CHECKSUM_AGG()` / `CHECKSUM()` / `BINARY_CHECKSUM()`
- [ ] `STDEV()` `STDEVP()` `VAR()` `VARP()`
- [ ] `APPROX_COUNT_DISTINCT()` / `APPROX_PERCENTILE_CONT()` / `APPROX_PERCENTILE_DISC()`
- [ ] `GROUPING_ID()`
- [ ] `PERCENTILE_CONT()` / `PERCENTILE_DISC()` with `WITHIN GROUP (...) OVER (PARTITION BY ...)`

### System and introspection
- [ ] `@@ROWCOUNT` `@@ERROR` `@@IDENTITY` `@@TRANCOUNT` `@@SPID` `@@VERSION` `@@SERVERNAME` `@@FETCH_STATUS`
- [ ] `ROWCOUNT_BIG()` `SCOPE_IDENTITY()` `IDENT_CURRENT()`
- [ ] `ERROR_NUMBER()` `ERROR_MESSAGE()` `ERROR_SEVERITY()` `ERROR_STATE()` `ERROR_LINE()` `ERROR_PROCEDURE()`
- [ ] `OBJECT_ID()` `OBJECT_NAME()` `DB_ID()` `DB_NAME()` `SCHEMA_NAME()` `COL_LENGTH()`
- [ ] `NEWID()` / `NEWSEQUENTIALID()`
- [ ] `SESSION_CONTEXT()` / `CONTEXT_INFO()`
- [ ] `USER_NAME()` `SUSER_SNAME()` `ORIGINAL_LOGIN()`

### Vector (2025)
- [ ] `VECTOR_DISTANCE()` `VECTOR_NORM()` `VECTOR_NORMALIZE()`
- [ ] `VECTOR_SEARCH()` table-valued function
- [ ] `AI_GENERATE_EMBEDDINGS(... USE MODEL model)` / `AI_GENERATE_CHUNKS()`

### CLR and type methods
- [ ] Instance method calls on UDT/spatial/hierarchyid values (`geo.STDistance(@p)`, `node.GetAncestor(1)`)
- [ ] Static method calls (`geography::Point(lat, lon, 4326)`, `hierarchyid::GetRoot()`)
- [ ] Property access on UDT values (`@p.X`, `@p.STSrid`)

## Query expressions
- [ ] `SELECT TOP (n) [PERCENT] [WITH TIES]` (and legacy `TOP n` without parentheses)
- [ ] Variable assignment in select lists (`SELECT @v = expr, @w = expr FROM ...`)
- [ ] `SELECT ... INTO new_table [ON filegroup] FROM ...`
- [ ] `alias = expr` select-list alias form and string-literal aliases (`expr AS 'alias'`)
- [ ] Table hints (`FROM t WITH (NOLOCK, READPAST, UPDLOCK, HOLDLOCK, INDEX(ix), FORCESEEK, ...)`)
- [ ] `OPTION (RECOMPILE, MAXDOP n, OPTIMIZE FOR (...), USE HINT ('...'), LABEL = '...', ...)` query hints
- [ ] Join hints (`INNER HASH JOIN`, `LEFT MERGE JOIN`, `LOOP`, `REMOTE`)
- [ ] `CROSS APPLY` / `OUTER APPLY`
- [ ] `PIVOT (agg(col) FOR col IN ([a], [b])) AS p`
- [ ] `UNPIVOT (val FOR col IN ([a], [b])) AS u`
- [ ] `FOR XML {RAW [('el')]|AUTO|EXPLICIT|PATH [('el')]} [, ROOT('r')] [, TYPE] [, ELEMENTS [XSINIL|ABSENT]] [, XMLSCHEMA]`
- [ ] `FOR JSON {AUTO|PATH} [, ROOT('r')] [, INCLUDE_NULL_VALUES] [, WITHOUT_ARRAY_WRAPPER]`
- [ ] `FOR BROWSE`
- [ ] Temporal queries (`FOR SYSTEM_TIME {AS OF|FROM ... TO|BETWEEN ... AND|CONTAINED IN (...)|ALL}`)
- [ ] `OFFSET n ROWS FETCH {FIRST|NEXT} n ROWS ONLY` as the only `FETCH` form (no `WITH TIES`/`PERCENT` here)
- [ ] `WINDOW` clause and named windows (2022)
- [ ] Multi-part names (`server.database.schema.object`, `database..object`, `.schema.object`)
- [ ] Derived table column aliases without `AS` and `VALUES` row constructors as derived tables
- [ ] `TABLESAMPLE [SYSTEM] (n {PERCENT|ROWS}) [REPEATABLE (seed)]`
- [ ] `GROUP BY ALL` (deprecated) and `GROUP BY ... WITH ROLLUP|CUBE`
- [ ] `COMPUTE` / `COMPUTE BY` (legacy, parse-only)

## DML
- [ ] `OUTPUT inserted.*|deleted.* [INTO {@table|table} [(cols)]]` on `INSERT` / `UPDATE` / `DELETE` / `MERGE`
- [ ] `$action` in `MERGE ... OUTPUT`
- [ ] `INSERT TOP (n)` / `UPDATE TOP (n)` / `DELETE TOP (n)`
- [ ] `UPDATE t SET ... FROM t JOIN ...`
- [ ] `DELETE [FROM] t FROM t JOIN ...` (second `FROM`)
- [ ] `UPDATE ... SET @v = col = expr` combined variable/column assignment
- [ ] `UPDATE ... SET col.WRITE(expr, offset, length)` partial LOB updates
- [ ] `UPDATE ... SET udt_col.Method(...)` mutator calls
- [ ] `INSERT INTO t EXEC[UTE] proc ...` / `INSERT ... EXEC ('sql')`
- [ ] `INSERT` without `INTO`
- [ ] `MERGE ... WHEN NOT MATCHED [BY TARGET]` / `WHEN NOT MATCHED BY SOURCE` with extra `AND` conditions
- [ ] Table hints on DML targets (`UPDATE t WITH (ROWLOCK)`, `MERGE t WITH (HOLDLOCK)`)
- [ ] `WHERE CURRENT OF [GLOBAL] cursor` with T-SQL cursor names and variables
- [ ] `BULK INSERT table FROM 'file' WITH (FORMAT = 'CSV', FIELDTERMINATOR = ..., ...)`
- [ ] `TRUNCATE TABLE ... WITH (PARTITIONS (1, 3 TO 5))`

## Schema / DDL

### Tables
- [ ] `IDENTITY [(seed, increment)]` column property
- [ ] Computed columns (`col AS expr [PERSISTED [NOT NULL]]`)
- [ ] `ON {filegroup|partition_scheme(col)|"default"}` / `TEXTIMAGE_ON` / `FILESTREAM_ON`
- [ ] Column options `FILESTREAM` `SPARSE` `ROWGUIDCOL` `COLUMN_SET FOR ALL_SPARSE_COLUMNS` `COLLATE`
- [ ] `WITH (DATA_COMPRESSION = {NONE|ROW|PAGE|COLUMNSTORE} [ON PARTITIONS (...)], ...)`
- [ ] System-versioned temporal tables (`PERIOD FOR SYSTEM_TIME (start, end)`, `GENERATED ALWAYS AS ROW START|END [HIDDEN]`, `WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = ..., HISTORY_RETENTION_PERIOD = ...))`)
- [ ] Memory-optimized tables (`WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA)`)
- [ ] Ledger tables (`WITH (LEDGER = ON [(APPEND_ONLY = ON)])`)
- [ ] Graph tables (`CREATE TABLE ... AS NODE` / `AS EDGE`)
- [ ] `CREATE TABLE #temp` / `##temp` and table variables in `DECLARE`
- [ ] `CREATE OR ALTER` for views, procedures, functions, triggers
- [ ] `DROP {TABLE|VIEW|PROCEDURE|...} IF EXISTS a, b, c`
- [ ] `ALTER TABLE ... ALTER COLUMN col type [NULL|NOT NULL] [WITH (ONLINE = ON)]`
- [ ] `ALTER TABLE ... ADD col ...` without `COLUMN` keyword, multiple comma-separated adds
- [ ] `ALTER TABLE ... DROP [COLUMN|CONSTRAINT] a, b`
- [ ] `ALTER TABLE ... [WITH {CHECK|NOCHECK}] {CHECK|NOCHECK} CONSTRAINT {ALL|name}`
- [ ] `ALTER TABLE ... {ENABLE|DISABLE} TRIGGER {ALL|name}`
- [ ] `ALTER TABLE ... SWITCH [PARTITION n] TO target [PARTITION m]`
- [ ] `ALTER TABLE ... SET (LOCK_ESCALATION = ..., SYSTEM_VERSIONING = ...)`
- [ ] `ALTER TABLE ... REBUILD [PARTITION = ...] WITH (...)`
- [ ] `sp_rename` style renames via `EXEC` (no dedicated `RENAME` syntax)

### Partitioning
- [ ] `CREATE PARTITION FUNCTION fn (type) AS RANGE {LEFT|RIGHT} FOR VALUES (...)`
- [ ] `CREATE PARTITION SCHEME ps AS PARTITION fn [ALL] TO (filegroups)`
- [ ] `ALTER PARTITION FUNCTION ... {SPLIT|MERGE} RANGE (value)`
- [ ] `ALTER PARTITION SCHEME ... NEXT USED filegroup`
- [ ] `$PARTITION.fn(expr)`

### Indexes
- [ ] `CREATE [UNIQUE] {CLUSTERED|NONCLUSTERED} INDEX`
- [ ] `CREATE [CLUSTERED|NONCLUSTERED] COLUMNSTORE INDEX` (with `ORDER (...)`)
- [ ] Filtered indexes (`CREATE INDEX ... WHERE predicate`)
- [ ] `INCLUDE (...)`
- [ ] `WITH (FILLFACTOR, PAD_INDEX, ONLINE, SORT_IN_TEMPDB, DROP_EXISTING, RESUMABLE, MAXDOP, ...)`
- [ ] `CREATE [PRIMARY] XML INDEX ... [USING XML INDEX ... FOR {VALUE|PATH|PROPERTY}]`
- [ ] `CREATE SPATIAL INDEX ... USING {GEOMETRY_GRID|GEOGRAPHY_GRID|...}`
- [ ] `CREATE JSON INDEX` / `CREATE VECTOR INDEX` (2025)
- [ ] Memory-optimized hash indexes (`INDEX ix HASH (col) WITH (BUCKET_COUNT = n)`)
- [ ] Inline index definitions inside `CREATE TABLE`
- [ ] `ALTER INDEX {name|ALL} ON t {REBUILD|REORGANIZE|DISABLE|RESUME|PAUSE|ABORT|SET (...)}`
- [ ] `DROP INDEX ix ON t` / legacy `DROP INDEX t.ix`
- [ ] `CREATE STATISTICS` / `UPDATE STATISTICS ... WITH FULLSCAN` / `DROP STATISTICS`

### Views
- [ ] `WITH SCHEMABINDING` / `ENCRYPTION` / `VIEW_METADATA`
- [ ] Indexed views (`CREATE UNIQUE CLUSTERED INDEX` on a schema-bound view)

### Sequences and synonyms
- [ ] `NEXT VALUE FOR seq [OVER (ORDER BY ...)]`
- [ ] `sp_sequence_get_range` via `EXEC`
- [ ] `CREATE SYNONYM name FOR [server.][db.][schema.]object`

### Databases, schemas, and storage
- [ ] `CREATE DATABASE ... ON [PRIMARY] (NAME = ..., FILENAME = ..., SIZE = ...) LOG ON (...) [COLLATE ...] [WITH ...]`
- [ ] `CREATE DATABASE ... AS SNAPSHOT OF` / `FOR ATTACH`
- [ ] `ALTER DATABASE ... {SET option|ADD FILE|ADD FILEGROUP|MODIFY FILE|MODIFY NAME}`
- [ ] `ALTER DATABASE SCOPED CONFIGURATION SET ...`
- [ ] `CREATE SCHEMA name [AUTHORIZATION owner] [schema_element ...]`
- [ ] `ALTER SCHEMA target TRANSFER [OBJECT::]source`
- [ ] `ALTER AUTHORIZATION ON class::object TO principal`

### Other objects
- [ ] `CREATE XML SCHEMA COLLECTION`
- [ ] `CREATE ASSEMBLY ... FROM ... WITH PERMISSION_SET = ...`
- [ ] CLR modules (`CREATE PROCEDURE|FUNCTION|TRIGGER|AGGREGATE ... EXTERNAL NAME a.c.m`)
- [ ] Legacy `CREATE DEFAULT` / `CREATE RULE` and `sp_bindefault` / `sp_bindrule`
- [ ] PolyBase (`CREATE EXTERNAL DATA SOURCE`, `CREATE EXTERNAL FILE FORMAT`, `CREATE EXTERNAL TABLE`, `CREATE EXTERNAL TABLE AS SELECT`)
- [ ] `CREATE EXTERNAL MODEL` (2025)

### Graph tables
- [ ] `AS NODE` / `AS EDGE` table declarations and `$node_id` `$edge_id` `$from_id` `$to_id` pseudo-columns
- [ ] `MATCH` patterns (`a-(e)->b`, `a<-(e)-b`, `SHORTEST_PATH(...)` with `+` / `{1,n}` quantifiers)
- [ ] Graph aggregate helpers (`LAST_NODE()`, `STRING_AGG(...) WITHIN GROUP (GRAPH PATH)`)

## Constraints
- [ ] `PRIMARY KEY` / `UNIQUE` with `CLUSTERED|NONCLUSTERED` and `WITH (...)` index options
- [ ] Named inline `DEFAULT` constraints (`col int CONSTRAINT df_x DEFAULT 0`)
- [ ] `ALTER TABLE ... ADD [CONSTRAINT name] DEFAULT expr FOR col`
- [ ] `NOT FOR REPLICATION`
- [ ] `ALTER TABLE ... WITH NOCHECK ADD CONSTRAINT ...`
- [ ] Edge constraints (`CONSTRAINT ec CONNECTION (Node1 TO Node2) ON DELETE CASCADE`)

## Triggers
- [ ] `CREATE [OR ALTER] TRIGGER ... ON t [WITH ...] {FOR|AFTER|INSTEAD OF} {INSERT|UPDATE|DELETE}[, ...] [NOT FOR REPLICATION] AS ...`
- [ ] `inserted` / `deleted` pseudo-tables
- [ ] `UPDATE(col)` / `COLUMNS_UPDATED()` inside triggers
- [ ] DDL triggers (`ON {DATABASE|ALL SERVER} FOR {CREATE_TABLE|DDL_DATABASE_LEVEL_EVENTS|...}`) and `EVENTDATA()`
- [ ] Logon triggers (`ON ALL SERVER FOR LOGON`)
- [ ] `{ENABLE|DISABLE} TRIGGER {name|ALL} ON {object|DATABASE|ALL SERVER}`
- [ ] Event notifications (`CREATE EVENT NOTIFICATION ... TO SERVICE ...`)

## Procedural T-SQL
- [ ] `DECLARE @a type [= expr], @b type, @t TABLE (...), @c CURSOR`
- [ ] `SET @v = expr` and compound `SET @v += expr`
- [ ] `BEGIN ... END` statement blocks
- [ ] `IF cond stmt [ELSE stmt]` (single statement or block, no `THEN`/`END IF`)
- [ ] `WHILE cond stmt` / `BREAK` / `CONTINUE`
- [ ] `GOTO label` / `label:`
- [ ] `RETURN [expr]`
- [ ] `WAITFOR {DELAY 'hh:mm:ss'|TIME 'hh:mm:ss'|(RECEIVE ...)} [, TIMEOUT n]`
- [ ] `BEGIN TRY ... END TRY BEGIN CATCH ... END CATCH`
- [ ] `THROW [number, message, state]`
- [ ] `RAISERROR (msg, severity, state [, args]) [WITH LOG|NOWAIT|SETERROR]`
- [ ] `PRINT expr`
- [ ] `EXEC[UTE] [@ret =] proc [@p =] value [OUTPUT] [DEFAULT], ... [WITH RECOMPILE|RESULT SETS (...)]`
- [ ] `EXEC[UTE] ('sql' + @x) [AS {LOGIN|USER} = '...'] [AT linked_server]`
- [ ] `EXEC sp_executesql @stmt, N'@p int', @p = 1`
- [ ] Procedure numbers (`proc;2`) and module-name variables (`EXEC @procname`)
- [ ] `CREATE [OR ALTER] PROC[EDURE] name [@p type [VARYING] [= default] [OUT|OUTPUT] [READONLY], ...] [WITH {RECOMPILE|ENCRYPTION|EXECUTE AS ...}] [FOR REPLICATION] AS ...`
- [ ] Scalar functions (`CREATE FUNCTION ... RETURNS type [WITH {SCHEMABINDING|RETURNS NULL ON NULL INPUT|INLINE = ON}] AS BEGIN ... RETURN expr END`)
- [ ] Inline table-valued functions (`RETURNS TABLE AS RETURN (select)`)
- [ ] Multi-statement table-valued functions (`RETURNS @t TABLE (...) AS BEGIN ... RETURN END`)
- [ ] Natively compiled modules (`WITH NATIVE_COMPILATION, SCHEMABINDING AS BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'...') ... END`)
- [ ] T-SQL cursor declarations (`DECLARE c CURSOR [LOCAL|GLOBAL] [FORWARD_ONLY|SCROLL] [STATIC|KEYSET|DYNAMIC|FAST_FORWARD] [READ_ONLY|SCROLL_LOCKS|OPTIMISTIC] FOR select [FOR UPDATE [OF ...]]`)
- [ ] `OPEN` / `FETCH [NEXT|PRIOR|FIRST|LAST|ABSOLUTE n|RELATIVE n] FROM [GLOBAL] c INTO @a, @b` / `CLOSE` / `DEALLOCATE`
- [ ] `SET @cursor_var = CURSOR FOR ...`

## Security
- [ ] `CREATE LOGIN name {WITH PASSWORD = '...' [HASHED] [MUST_CHANGE], ...|FROM {WINDOWS|EXTERNAL PROVIDER|CERTIFICATE c|ASYMMETRIC KEY k}}`
- [ ] `CREATE USER name [{FOR|FROM} LOGIN l|WITHOUT LOGIN|FROM EXTERNAL PROVIDER] [WITH DEFAULT_SCHEMA = s]`
- [ ] `CREATE ROLE` / `CREATE SERVER ROLE` / `CREATE APPLICATION ROLE`
- [ ] `ALTER [SERVER] ROLE r {ADD|DROP} MEMBER m`
- [ ] `GRANT|DENY|REVOKE perm ON {OBJECT|SCHEMA|DATABASE|TYPE|XML SCHEMA COLLECTION|...}::name TO principal [WITH GRANT OPTION] [AS principal]`
- [ ] `DENY` statement
- [ ] `EXECUTE AS {CALLER|SELF|OWNER|USER = '...'|LOGIN = '...'}` / `REVERT [WITH COOKIE = @c]`
- [ ] Row-level security (`CREATE SECURITY POLICY ... ADD {FILTER|BLOCK} PREDICATE fn(col) ON t [AFTER INSERT|...] WITH (STATE = ON)`)
- [ ] Dynamic data masking (`col ... MASKED WITH (FUNCTION = 'partial(1,"xx",1)')`, `ALTER COLUMN ... ADD|DROP MASKED`)
- [ ] Always Encrypted (`ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = k, ENCRYPTION_TYPE = ..., ALGORITHM = '...')`, `CREATE COLUMN MASTER KEY` / `COLUMN ENCRYPTION KEY`)
- [ ] Keys and certificates (`CREATE MASTER KEY`, `CREATE CERTIFICATE`, `CREATE SYMMETRIC|ASYMMETRIC KEY`, `OPEN|CLOSE SYMMETRIC KEY`, `ADD SIGNATURE`)
- [ ] Credentials and auditing (`CREATE [DATABASE SCOPED] CREDENTIAL`, `CREATE SERVER AUDIT`, `CREATE DATABASE AUDIT SPECIFICATION`)

## Transactions and session
- [ ] `BEGIN TRAN[SACTION] [name|@var] [WITH MARK ['desc']]`
- [ ] `SAVE TRAN[SACTION] name`
- [ ] `COMMIT [TRAN[SACTION]] [name] [WITH (DELAYED_DURABILITY = ON)]` / `ROLLBACK [TRAN[SACTION]] [name]`
- [ ] `COMMIT WORK` / `ROLLBACK WORK`
- [ ] `BEGIN DISTRIBUTED TRAN[SACTION]`
- [ ] `SET` session options (`NOCOUNT` `XACT_ABORT` `ANSI_NULLS` `ANSI_PADDING` `ARITHABORT` `CONCAT_NULL_YIELDS_NULL` `DATEFIRST` `DATEFORMAT` `LANGUAGE` `LOCK_TIMEOUT` `DEADLOCK_PRIORITY` `ROWCOUNT` `TEXTSIZE` `IMPLICIT_TRANSACTIONS` `CONTEXT_INFO` `{ON|OFF}`)
- [ ] `SET TRANSACTION ISOLATION LEVEL {READ UNCOMMITTED|READ COMMITTED|REPEATABLE READ|SNAPSHOT|SERIALIZABLE}`
- [ ] `SET IDENTITY_INSERT t {ON|OFF}`
- [ ] Diagnostics options (`SET STATISTICS {IO|TIME|XML|PROFILE} ON`, `SET SHOWPLAN_{TEXT|ALL|XML} ON`, `SET NOEXEC ON`, `SET PARSEONLY ON`)
- [ ] `USE database`

## Administration and utility commands
- [ ] `DBCC command [(args)] [WITH options]` (`CHECKDB`, `SHRINKFILE`, `FREEPROCCACHE`, `INPUTBUFFER`, …)
- [ ] `BACKUP {DATABASE|LOG} name TO {DISK|URL} = '...' [WITH ...]`
- [ ] `RESTORE {DATABASE|LOG|HEADERONLY|FILELISTONLY|VERIFYONLY} ... FROM ... [WITH MOVE ..., NORECOVERY, ...]`
- [ ] `CHECKPOINT [duration]`
- [ ] `KILL {session_id|UOW} [WITH STATUSONLY]`
- [ ] `RECONFIGURE [WITH OVERRIDE]`
- [ ] `SHUTDOWN [WITH NOWAIT]`
- [ ] `ALTER SERVER CONFIGURATION SET ...`
- [ ] Availability groups and endpoints (`CREATE|ALTER AVAILABILITY GROUP`, `CREATE ENDPOINT`)
- [ ] Resource Governor (`CREATE RESOURCE POOL`, `CREATE WORKLOAD GROUP`, `ALTER RESOURCE GOVERNOR`)
- [ ] Extended events (`CREATE EVENT SESSION ... ON SERVER ADD EVENT ... ADD TARGET ...`)
- [ ] Legacy text/image statements (`READTEXT` `WRITETEXT` `UPDATETEXT`)
- [ ] `SETUSER` (legacy)

## Service Broker
- [ ] `CREATE MESSAGE TYPE` / `CREATE CONTRACT` / `CREATE QUEUE` / `CREATE SERVICE` / `CREATE ROUTE`
- [ ] `BEGIN DIALOG [CONVERSATION] @h FROM SERVICE s TO SERVICE 't' ON CONTRACT c [WITH ...]`
- [ ] `SEND ON CONVERSATION @h [MESSAGE TYPE m] (body)`
- [ ] `RECEIVE [TOP (n)] cols FROM queue [INTO @t] [WHERE ...]`
- [ ] `END CONVERSATION @h [WITH ERROR = n DESCRIPTION = '...' | WITH CLEANUP]`
- [ ] `GET CONVERSATION GROUP @g FROM queue`
- [ ] `MOVE CONVERSATION`

## Full text search
- [ ] `CONTAINS(col|(cols)|*, 'search condition' [, LANGUAGE l])`
- [ ] `FREETEXT(col|(cols)|*, 'text' [, LANGUAGE l])`
- [ ] `CONTAINSTABLE(t, col, '...' [, top_n])` / `FREETEXTTABLE(...)` with `KEY` / `RANK` columns
- [ ] `CREATE FULLTEXT CATALOG` / `ALTER FULLTEXT CATALOG`
- [ ] `CREATE FULLTEXT INDEX ON t (col [TYPE COLUMN c] [LANGUAGE l]) KEY INDEX ix [ON catalog] [WITH ...]`
- [ ] `CREATE FULLTEXT STOPLIST` / `CREATE SEARCH PROPERTY LIST`
- [ ] `SEMANTICKEYPHRASETABLE()` / `SEMANTICSIMILARITYTABLE()` / `SEMANTICSIMILARITYDETAILSTABLE()`

## System objects and pseudo-columns
- [ ] `sys.*` catalog views, `INFORMATION_SCHEMA`, and `dm_*` DMVs as ordinary multi-part names
- [ ] System stored procedures (`sp_*`, `xp_*`) called via `EXEC` without schema qualification
- [ ] `$IDENTITY` / `$ROWGUID` pseudo-columns
- [ ] `$PARTITION.fn(expr)`
- [ ] Graph pseudo-columns (`$node_id` `$edge_id` `$from_id` `$to_id`)
