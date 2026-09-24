# ISO SQL coverage (9075:1992–2023)

Vendor dialects: elsewhere. `[x]` = qlparse accepts it.

## Lexical

### Identifiers
- [x] Regular identifiers
- [x] Delimited identifiers
- [x] Unicode delimited identifiers (`U&`)
- [x] Character-set introducers

### Literals
- [x] Exact numeric (integer, decimal)
- [x] Approximate numeric (exponent)
- [x] Character string (`''` escape)
- [x] National / bit / hex string prefixes (`N` `B` `X`)
- [x] Unicode string (`U&`)
- [x] Date / time / timestamp literals
- [x] Interval literals and qualifiers
- [x] Boolean (`TRUE` `FALSE`)
- [x] `NULL`

### Comments and tokens
- [x] Line comments
- [x] Nested block comments
- [x] Simple comment alternatives in standard
- [x] Host parameters (`?`)
- [x] Embedded host names

## Data types

### Predefined
- [x] Exact numeric (`NUMERIC` `DECIMAL` `SMALLINT` `INTEGER` `BIGINT`)
- [x] Approximate numeric (`FLOAT` `REAL` `DOUBLE PRECISION`)
- [x] `DOUBLE PRECISION` as type name
- [x] Character (`CHAR` `VARCHAR` `CLOB` `CHARACTER LARGE OBJECT`)
- [x] `CHAR VARYING` / `CHARACTER VARYING`
- [x] National character (`NCHAR` `NVARCHAR` `NCLOB`)
- [x] Binary (`BINARY` `VARBINARY` `BLOB`)
- [x] `BOOLEAN`
- [x] `DATE` `TIME` `TIMESTAMP` `[WITH TIME ZONE]`
- [x] `INTERVAL`
- [x] `XML`
- [x] `JSON`
- [x] `ROW`
- [x] `ARRAY` constructor
- [x] `ARRAY` type and typed constructors
- [x] `MULTISET`
- [x] `REF`
- [x] User-defined distinct / structured types
- [x] `MD-ARRAY`

### Type operations
- [x] `CAST`
- [x] `TREAT`
- [x] `NEXT VALUE FOR`

## Scalar expressions

### Operators
- [x] Arithmetic `+ - * /`
- [x] Unary `+ -`
- [x] Concatenation
- [x] Field / member reference
- [x] Parentheses
- [x] Row constructors

### Conditionals and null
- [x] `CASE` (simple and searched)
- [x] `NULLIF` as special form
- [x] `COALESCE` as special form
- [x] `COALESCE` as function call

### String / datetime functions
- [x] `SUBSTRING`
- [x] `TRIM`
- [x] `POSITION`
- [x] `EXTRACT`
- [x] `CONVERT … USING`
- [x] `TRANSLATE … USING`
- [x] `UPPER` `LOWER` `OVERLAY` `CHAR_LENGTH` `OCTET_LENGTH` `BIT_LENGTH` as special forms
- [x] `NORMALIZE`
- [x] `FLOOR` `CEIL` `POWER` `SQRT` `LN` `EXP` `MOD` `ABS` `WIDTH_BUCKET` as special forms

### Datetime value functions
- [x] `CURRENT_DATE`
- [x] `CURRENT_TIME` `[precision]`
- [x] `CURRENT_TIMESTAMP` `[precision]`
- [x] `LOCALTIME` `[precision]`
- [x] `LOCALTIMESTAMP` `[precision]`
- [x] `USER` `CURRENT_USER` `SESSION_USER` `SYSTEM_USER`
- [x] `CURRENT_ROLE` `CURRENT_CATALOG` `CURRENT_SCHEMA` `CURRENT_PATH`

### Collections
- [x] `ARRAY[…]`
- [x] `ARRAY` subquery
- [x] `MULTISET` constructors and ops
- [x] `UNNEST`
- [x] `CARDINALITY` `ELEMENT` `ABSENT`

### Collation
- [x] `COLLATE`

## Predicates

- [x] Comparison `= <> < > <= >=`
- [x] Quantified comparison `ALL` `ANY` `SOME`
- [x] `BETWEEN` / `NOT BETWEEN`
- [x] `IN` list and subquery / `NOT IN`
- [x] `LIKE` / `NOT LIKE` `[ESCAPE]`
- [x] `SIMILAR TO` / `NOT SIMILAR`
- [x] `IS [NOT] NULL`
- [x] `IS [NOT] TRUE` `FALSE` `UNKNOWN`
- [x] `IS [NOT] DISTINCT FROM`
- [x] `IS [NOT] NORMALIZED`
- [x] `EXISTS`
- [x] `UNIQUE`
- [x] `MATCH` `[UNIQUE]` `[SIMPLE|PARTIAL|FULL]`
- [x] `OVERLAPS`
- [x] `DISTINCT` predicate
- [x] Quantified subquery vs comparison completeness

## Query expressions

### WITH
- [x] `WITH`
- [x] `WITH RECURSIVE`
- [x] CTE column lists
- [x] `SEARCH` / `CYCLE`

### SELECT
- [x] `SELECT [DISTINCT|ALL]`
- [x] Select list, `*`, qualified `*`
- [x] Column aliases (`AS` and implicit)
- [x] `SELECT … INTO`

### FROM
- [x] Table names (catalog.schema.table)
- [x] Correlation names and column lists
- [x] Derived tables
- [x] Parenthesized joined tables
- [x] Comma-separated table references
- [x] `LATERAL`
- [x] `TABLESAMPLE`
- [x] `ONLY` (typed tables)
- [x] `UNNEST` in `FROM`
- [x] Table functions
- [x] `TABLE` value constructor in `FROM`

### JOIN
- [x] `INNER JOIN`
- [x] `LEFT` `RIGHT` `FULL` `[OUTER] JOIN`
- [x] `CROSS JOIN`
- [x] `NATURAL` joins
- [x] `UNION JOIN`
- [x] `ON`
- [x] `USING`

### Grouping
- [x] `GROUP BY` expression list
- [x] `HAVING`
- [x] `ROLLUP`
- [x] `CUBE`
- [x] `GROUPING SETS`
- [x] `GROUPING()`
- [x] `FILTER (WHERE …)` on function calls

### Ordering and paging
- [x] `ORDER BY` `[ASC|DESC]`
- [x] `NULLS FIRST` `NULLS LAST`
- [x] `FETCH FIRST` / `FETCH NEXT` `{ROW|ROWS} ONLY`
- [x] `OFFSET n {ROW|ROWS}`
- [x] `PERCENT` / `WITH TIES`

### Set operations
- [x] `UNION` `[ALL]`
- [x] `EXCEPT` `[ALL]`
- [x] `INTERSECT` `[ALL]`
- [x] `CORRESPONDING [BY]`
- [x] Parenthesized query expressions
- [x] `VALUES`

### Subqueries
- [x] Scalar subquery
- [x] `IN` subquery
- [x] `EXISTS` / `UNIQUE` subquery
- [x] Quantified subquery
- [x] Derived table `LATERAL` subquery

### Window
- [x] `OVER (…)`
- [x] `PARTITION BY`
- [x] Window `ORDER BY`
- [x] `ROWS` / `RANGE` / `GROUPS` frames
- [x] Frame exclusions
- [x] Named `WINDOW` clause
- [x] `RANK` `DENSE_RANK` `PERCENT_RANK` `CUME_DIST` `ROW_NUMBER`
- [x] `LAG` `LEAD` `FIRST_VALUE` `LAST_VALUE` `NTH_VALUE` `NTILE`
- [x] Nested window functions

### Locks
- [x] `FOR UPDATE [OF …]`
- [x] `FOR READ ONLY`
- [x] `FOR SHARE` (not ISO)

### Recursion extras
- [x] Linear vs general recursion limits as syntax

## DML

- [x] `INSERT … VALUES`
- [x] `INSERT … SELECT`
- [x] `INSERT … DEFAULT VALUES`
- [x] `INSERT … OVERRIDING`
- [x] `UPDATE` searched
- [x] `UPDATE` positioned
- [x] `DELETE` searched
- [x] `DELETE` positioned
- [x] `MERGE`
- [x] `TRUNCATE`
- [x] `SET` row assignments

## Schema / DDL

### Catalogs and schemas
- [x] `CREATE` `ALTER` `DROP SCHEMA`
- [x] `CREATE` `DROP CATALOG` (not in Foundation)
- [x] Path / default schema session objects

### Tables
- [x] `CREATE TABLE`
- [x] `CREATE TABLE AS`
- [x] `CREATE TABLE OF` (typed)
- [x] `ALTER TABLE`
- [x] `DROP TABLE`
- [x] Temporary tables (`GLOBAL` `LOCAL`)
- [x] Generated / identity columns
- [x] Like-table `LIKE`

### Views
- [x] `CREATE VIEW`
- [x] `WITH [CASCADED|LOCAL] CHECK OPTION`
- [x] `ALTER VIEW`
- [x] `DROP VIEW`

### Domains and types
- [x] `CREATE` `ALTER` `DROP DOMAIN`
- [x] `CREATE TYPE` distinct
- [x] `CREATE TYPE` structured
- [x] `CREATE TYPE` array / multiset
- [x] `DROP TYPE`
- [x] `CREATE ORDERING` `CREATE CAST` `CREATE TRANSFORM`

### Other schema objects
- [x] `CREATE` `DROP ASSERTION`
- [x] `CREATE` `DROP CHARACTER SET`
- [x] `CREATE` `DROP COLLATION`
- [x] `CREATE` `DROP TRANSLATION`
- [x] `CREATE` `DROP SEQUENCE`
- [x] `CREATE` `ALTER` `DROP INDEX` (not ISO Foundation)
- [x] `COMMENT` (not ISO)

## Constraints

- [x] `NOT NULL`
- [x] `UNIQUE`
- [x] `PRIMARY KEY`
- [x] `FOREIGN KEY` and referential actions
- [x] `MATCH SIMPLE|PARTIAL|FULL` on FKs
- [x] `CHECK`
- [x] `DEFAULT`
- [x] `DEFERRABLE` / `INITIALLY DEFERRED|IMMEDIATE`
- [x] Named constraints
- [x] Unique null treatment (`UNIQUE NULLS [NOT] DISTINCT`)

## Privileges and roles

- [x] `GRANT` privilege
- [x] `REVOKE` privilege
- [x] `GRANT` / `REVOKE` role
- [x] `CREATE ROLE` `DROP ROLE`
- [x] `WITH GRANT OPTION` `WITH ADMIN OPTION`
- [x] `SET ROLE`

## Transactions and sessions

- [x] `START TRANSACTION` / `SET TRANSACTION`
- [x] Isolation levels
- [x] `READ ONLY` `READ WRITE`
- [x] `DIAGNOSTICS SIZE`
- [x] `COMMIT` `[WORK]` `[AND [NO] CHAIN]`
- [x] `ROLLBACK` `[WORK]` `[AND [NO] CHAIN]`
- [x] `SAVEPOINT` `RELEASE SAVEPOINT` `ROLLBACK TO`
- [x] `SET CONSTRAINTS`
- [x] `SET SESSION AUTHORIZATION`
- [x] `SET SESSION CHARACTERISTICS`
- [x] `SET NAMES` / charset/collation session
- [x] `SET TIME ZONE`
- [x] `SET CATALOG` `SET SCHEMA` `SET PATH`

## Connections

- [x] `CONNECT`
- [x] `DISCONNECT`
- [x] `SET CONNECTION`

## Cursors

- [x] `DECLARE CURSOR`
- [x] Sensitivity / scroll / hold / return
- [x] `OPEN` `FETCH` `CLOSE`
- [x] `UPDATE`/`DELETE` where current of
- [x] `ALLOCATE` / `DEALLOCATE` (dynamic)

## Dynamic SQL

- [x] `PREPARE` `EXECUTE` `EXECUTE IMMEDIATE`
- [x] `DESCRIBE`
- [x] Dynamic `DECLARE CURSOR`
- [x] `ALLOCATE DESCRIPTOR`

## Diagnostics

- [x] `GET DIAGNOSTICS`
- [x] `SIGNAL` `RESIGNAL` (PSM)
- [x] SQLSTATE classes as syntax (none)

## Triggers and routines

- [x] `CREATE TRIGGER` (`BEFORE` `AFTER` `INSTEAD OF`)
- [x] `DROP TRIGGER`
- [x] `CREATE FUNCTION` `CREATE PROCEDURE` `CREATE METHOD`
- [x] `ALTER` / `DROP` routine
- [x] `CALL`
- [x] `RETURN`
- [x] External vs SQL routines
- [x] Determinism / null-call / data access clauses

## SQL/PSM (Part 4)

- [x] Compound `BEGIN … END`
- [x] Variables and `SET`
- [x] `IF` `CASE` statements
- [x] `LOOP` `WHILE` `REPEAT` `FOR`
- [x] `LEAVE` `ITERATE`
- [x] Condition handlers
- [x] `SIGNAL` `RESIGNAL`

## Typed tables and UDTs (1999)

- [x] Typed tables
- [x] `REF` values and `DEREF`
- [x] `SCOPE`
- [x] Table inheritance `UNDER`
- [x] Methods and invocations
- [x] `SPECIFICTYPE`

## Sequences and identity (2003)

- [x] `CREATE SEQUENCE`
- [x] `NEXT VALUE FOR`
- [x] Identity column options
- [x] `OVERRIDING SYSTEM|USER VALUE`

## Temporal (2011)

- [x] Application-time periods
- [x] System-versioned tables
- [x] `FOR SYSTEM_TIME AS OF`
- [x] `FOR SYSTEM_TIME BETWEEN` / `FROM … TO` / `ALL`
- [x] Period predicates

## SQL/XML (Part 14)

- [x] `XML` type
- [x] `XMLPARSE` `XMLSERIALIZE`
- [x] `XMLELEMENT` `XMLATTRIBUTES` `XMLFOREST` `XMLCONCAT` `XMLAGG`
- [x] `XMLCOMMENT` `XMLPI` `XMLDOCUMENT`
- [x] `XMLQUERY` `XMLEXISTS` `XMLTABLE` `XMLCAST` `XMLVALIDATE`
- [x] `IS [NOT] DOCUMENT` `IS [NOT] CONTENT`
- [x] XML namespaces in SQL

## JSON (2016–2023)

- [x] `JSON` type
- [x] `JSON_OBJECT` `JSON_ARRAY`
- [x] `JSON_OBJECTAGG` `JSON_ARRAYAGG`
- [x] `JSON_VALUE` `JSON_QUERY` `JSON_EXISTS`
- [x] `JSON_TABLE`
- [x] `JSON_SERIALIZE` `JSON_SCALAR`
- [x] `IS [NOT] JSON`
- [x] JSON path / simplified accessor
- [x] `JSON` predicate extras (2023)

## Row pattern matching (2016)

- [x] `MATCH_RECOGNIZE`
- [x] Pattern variables and `DEFINE`
- [x] `MEASURES`
- [x] `ONE ROW` / `ALL ROWS PER MATCH`
- [x] `AFTER MATCH SKIP`
- [x] `SUBSET`

## Polymorphic table functions (2016)

- [x] PTF invocation syntax
- [x] Copartitioning / row semantics vs table semantics

## SQL/MDA (2019)

- [x] `MDARRAY` type
- [x] MDA constructors and slicing
- [x] MDA aggregate / subset ops

## SQL/PGQ (2023)

- [x] `CREATE PROPERTY GRAPH` `DROP PROPERTY GRAPH`
- [x] `GRAPH_TABLE`
- [x] Graph pattern `MATCH`
- [x] Path and quantified path patterns
- [x] Graph labels and properties in SQL

## Direct SQL and modules

- [x] Direct SQL statement list (`;` terminated scripts)
- [x] Optional trailing `;` on one statement
- [x] SQL-client module language
- [x] Embedded SQL host program syntax
- [x] `DECLARE SECTION`

## Information schema (Part 11)

- [x] `INFORMATION_SCHEMA` as ordinary identifiers (no extra syntax)
