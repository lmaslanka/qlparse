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
- [ ] `UPPER` `LOWER` `OVERLAY` `CHAR_LENGTH` `OCTET_LENGTH` `BIT_LENGTH` as special forms
- [ ] `NORMALIZE`
- [ ] `FLOOR` `CEIL` `POWER` `SQRT` `LN` `EXP` `MOD` `ABS` `WIDTH_BUCKET` as special forms

### Datetime value functions
- [x] `CURRENT_DATE`
- [x] `CURRENT_TIME` `[precision]`
- [x] `CURRENT_TIMESTAMP` `[precision]`
- [ ] `LOCALTIME` `[precision]`
- [ ] `LOCALTIMESTAMP` `[precision]`
- [x] `USER` `CURRENT_USER` `SESSION_USER` `SYSTEM_USER`
- [ ] `CURRENT_ROLE` `CURRENT_CATALOG` `CURRENT_SCHEMA` `CURRENT_PATH`

### Collections
- [x] `ARRAY[…]`
- [ ] `ARRAY` subquery
- [ ] `MULTISET` constructors and ops
- [ ] `UNNEST`
- [ ] `CARDINALITY` `ELEMENT` `ABSENT`

### Collation
- [x] `COLLATE`

## Predicates

- [x] Comparison `= <> < > <= >=`
- [x] Quantified comparison `ALL` `ANY` `SOME`
- [x] `BETWEEN` / `NOT BETWEEN`
- [x] `IN` list and subquery / `NOT IN`
- [x] `LIKE` / `NOT LIKE` `[ESCAPE]`
- [ ] `SIMILAR TO` / `NOT SIMILAR`
- [x] `IS [NOT] NULL`
- [x] `IS [NOT] TRUE` `FALSE` `UNKNOWN`
- [ ] `IS [NOT] DISTINCT FROM`
- [ ] `IS [NOT] NORMALIZED`
- [x] `EXISTS`
- [x] `UNIQUE`
- [x] `MATCH` `[UNIQUE]` `[SIMPLE|PARTIAL|FULL]`
- [x] `OVERLAPS`
- [ ] `DISTINCT` predicate
- [ ] Quantified subquery vs comparison completeness

## Query expressions

### WITH
- [x] `WITH`
- [x] `WITH RECURSIVE`
- [x] CTE column lists
- [ ] `SEARCH` / `CYCLE`

### SELECT
- [x] `SELECT [DISTINCT|ALL]`
- [x] Select list, `*`, qualified `*`
- [x] Column aliases (`AS` and implicit)
- [ ] `SELECT … INTO`

### FROM
- [x] Table names (catalog.schema.table)
- [x] Correlation names and column lists
- [x] Derived tables
- [x] Parenthesized joined tables
- [x] Comma-separated table references
- [ ] `LATERAL`
- [ ] `TABLESAMPLE`
- [ ] `ONLY` (typed tables)
- [ ] `UNNEST` in `FROM`
- [ ] Table functions
- [ ] `TABLE` value constructor in `FROM`

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
- [ ] `ROLLUP`
- [ ] `CUBE`
- [ ] `GROUPING SETS`
- [ ] `GROUPING()`
- [x] `FILTER (WHERE …)` on function calls

### Ordering and paging
- [x] `ORDER BY` `[ASC|DESC]`
- [ ] `NULLS FIRST` `NULLS LAST`
- [ ] `FETCH FIRST` / `FETCH NEXT` `{ROW|ROWS} ONLY`
- [ ] `OFFSET n {ROW|ROWS}`
- [ ] `PERCENT` / `WITH TIES`

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
- [ ] Derived table `LATERAL` subquery

### Window
- [ ] `OVER (…)`
- [ ] `PARTITION BY`
- [ ] Window `ORDER BY`
- [ ] `ROWS` / `RANGE` / `GROUPS` frames
- [ ] Frame exclusions
- [ ] Named `WINDOW` clause
- [ ] `RANK` `DENSE_RANK` `PERCENT_RANK` `CUME_DIST` `ROW_NUMBER`
- [ ] `LAG` `LEAD` `FIRST_VALUE` `LAST_VALUE` `NTH_VALUE` `NTILE`
- [ ] Nested window functions

### Locks
- [x] `FOR UPDATE [OF …]`
- [x] `FOR READ ONLY`
- [ ] `FOR SHARE` (not ISO)

### Recursion extras
- [ ] Linear vs general recursion limits as syntax

## DML

- [ ] `INSERT … VALUES`
- [x] `INSERT … SELECT`
- [ ] `INSERT … DEFAULT VALUES`
- [ ] `INSERT … OVERRIDING`
- [ ] `UPDATE` searched
- [ ] `UPDATE` positioned
- [ ] `DELETE` searched
- [ ] `DELETE` positioned
- [ ] `MERGE`
- [ ] `TRUNCATE`
- [ ] `SET` row assignments

## Schema / DDL

### Catalogs and schemas
- [ ] `CREATE` `ALTER` `DROP SCHEMA`
- [ ] `CREATE` `DROP CATALOG` (where standard)
- [ ] Path / default schema session objects

### Tables
- [ ] `CREATE TABLE`
- [ ] `CREATE TABLE AS`
- [ ] `CREATE TABLE OF` (typed)
- [ ] `ALTER TABLE`
- [ ] `DROP TABLE`
- [ ] Temporary tables (`GLOBAL` `LOCAL`)
- [ ] Generated / identity columns
- [ ] Like-table `LIKE`

### Views
- [ ] `CREATE VIEW`
- [ ] `WITH [CASCADED|LOCAL] CHECK OPTION`
- [ ] `ALTER VIEW`
- [ ] `DROP VIEW`

### Domains and types
- [ ] `CREATE` `ALTER` `DROP DOMAIN`
- [ ] `CREATE TYPE` distinct
- [ ] `CREATE TYPE` structured
- [ ] `CREATE TYPE` array / multiset
- [ ] `DROP TYPE`
- [ ] `CREATE ORDERING` `CREATE CAST` `CREATE TRANSFORM`

### Other schema objects
- [ ] `CREATE` `DROP ASSERTION`
- [ ] `CREATE` `DROP CHARACTER SET`
- [ ] `CREATE` `DROP COLLATION`
- [ ] `CREATE` `DROP TRANSLATION`
- [ ] `CREATE` `DROP SEQUENCE`
- [ ] `CREATE` `ALTER` `DROP INDEX` (not ISO Foundation)
- [ ] `COMMENT` (not ISO)

## Constraints

- [ ] `NOT NULL`
- [ ] `UNIQUE`
- [ ] `PRIMARY KEY`
- [ ] `FOREIGN KEY` and referential actions
- [ ] `MATCH SIMPLE|PARTIAL|FULL` on FKs
- [ ] `CHECK`
- [ ] `DEFAULT`
- [ ] `DEFERRABLE` / `INITIALLY DEFERRED|IMMEDIATE`
- [ ] Named constraints
- [ ] Unique null treatment (`UNIQUE NULLS [NOT] DISTINCT`)

## Privileges and roles

- [ ] `GRANT` privilege
- [ ] `REVOKE` privilege
- [ ] `GRANT` / `REVOKE` role
- [ ] `CREATE ROLE` `DROP ROLE`
- [ ] `WITH GRANT OPTION` `WITH ADMIN OPTION`
- [ ] `SET ROLE`

## Transactions and sessions

- [ ] `START TRANSACTION` / `SET TRANSACTION`
- [ ] Isolation levels
- [ ] `READ ONLY` `READ WRITE`
- [ ] `DIAGNOSTICS SIZE`
- [ ] `COMMIT` `[WORK]` `[AND [NO] CHAIN]`
- [ ] `ROLLBACK` `[WORK]` `[AND [NO] CHAIN]`
- [ ] `SAVEPOINT` `RELEASE SAVEPOINT` `ROLLBACK TO`
- [ ] `SET CONSTRAINTS`
- [ ] `SET SESSION AUTHORIZATION`
- [ ] `SET SESSION CHARACTERISTICS`
- [ ] `SET NAMES` / charset/collation session
- [ ] `SET TIME ZONE`
- [ ] `SET CATALOG` `SET SCHEMA` `SET PATH`

## Connections

- [ ] `CONNECT`
- [ ] `DISCONNECT`
- [ ] `SET CONNECTION`

## Cursors

- [ ] `DECLARE CURSOR`
- [ ] Sensitivity / scroll / hold / return
- [ ] `OPEN` `FETCH` `CLOSE`
- [ ] `UPDATE`/`DELETE` where current of
- [ ] `ALLOCATE` / `DEALLOCATE` (dynamic)

## Dynamic SQL

- [ ] `PREPARE` `EXECUTE` `EXECUTE IMMEDIATE`
- [ ] `DESCRIBE`
- [ ] Dynamic `DECLARE CURSOR`
- [ ] `ALLOCATE DESCRIPTOR`

## Diagnostics

- [ ] `GET DIAGNOSTICS`
- [ ] `SIGNAL` `RESIGNAL` (PSM)
- [ ] SQLSTATE classes as syntax (none)

## Triggers and routines

- [ ] `CREATE TRIGGER` (`BEFORE` `AFTER` `INSTEAD OF`)
- [ ] `DROP TRIGGER`
- [ ] `CREATE FUNCTION` `CREATE PROCEDURE` `CREATE METHOD`
- [ ] `ALTER` / `DROP` routine
- [ ] `CALL`
- [ ] `RETURN`
- [ ] External vs SQL routines
- [ ] Determinism / null-call / data access clauses

## SQL/PSM (Part 4)

- [ ] Compound `BEGIN … END`
- [ ] Variables and `SET`
- [ ] `IF` `CASE` statements
- [ ] `LOOP` `WHILE` `REPEAT` `FOR`
- [ ] `LEAVE` `ITERATE`
- [ ] Condition handlers
- [ ] `SIGNAL` `RESIGNAL`

## Typed tables and UDTs (1999)

- [ ] Typed tables
- [ ] `REF` values and `DEREF`
- [ ] `SCOPE`
- [ ] Table inheritance `UNDER`
- [ ] Methods and invocations
- [ ] `SPECIFICTYPE`

## Sequences and identity (2003)

- [ ] `CREATE SEQUENCE`
- [ ] `NEXT VALUE FOR`
- [ ] Identity column options
- [ ] `OVERRIDING SYSTEM|USER VALUE`

## Temporal (2011)

- [ ] Application-time periods
- [ ] System-versioned tables
- [ ] `FOR SYSTEM_TIME AS OF`
- [ ] `FOR SYSTEM_TIME BETWEEN` / `FROM … TO` / `ALL`
- [ ] Period predicates

## SQL/XML (Part 14)

- [ ] `XML` type
- [ ] `XMLPARSE` `XMLSERIALIZE`
- [ ] `XMLELEMENT` `XMLATTRIBUTES` `XMLFOREST` `XMLCONCAT` `XMLAGG`
- [ ] `XMLCOMMENT` `XMLPI` `XMLDOCUMENT`
- [ ] `XMLQUERY` `XMLEXISTS` `XMLTABLE` `XMLCAST` `XMLVALIDATE`
- [ ] `IS [NOT] DOCUMENT` `IS [NOT] CONTENT`
- [ ] XML namespaces in SQL

## JSON (2016–2023)

- [ ] `JSON` type
- [ ] `JSON_OBJECT` `JSON_ARRAY`
- [ ] `JSON_OBJECTAGG` `JSON_ARRAYAGG`
- [ ] `JSON_VALUE` `JSON_QUERY` `JSON_EXISTS`
- [ ] `JSON_TABLE`
- [ ] `JSON_SERIALIZE` `JSON_SCALAR`
- [ ] `IS [NOT] JSON`
- [ ] JSON path / simplified accessor
- [ ] `JSON` predicate extras (2023)

## Row pattern matching (2016)

- [ ] `MATCH_RECOGNIZE`
- [ ] Pattern variables and `DEFINE`
- [ ] `MEASURES`
- [ ] `ONE ROW` / `ALL ROWS PER MATCH`
- [ ] `AFTER MATCH SKIP`
- [ ] `SUBSET`

## Polymorphic table functions (2016)

- [ ] PTF invocation syntax
- [ ] Copartitioning / row semantics vs table semantics

## SQL/MDA (2019)

- [ ] `MDARRAY` type
- [ ] MDA constructors and slicing
- [ ] MDA aggregate / subset ops

## SQL/PGQ (2023)

- [ ] `CREATE PROPERTY GRAPH` `DROP PROPERTY GRAPH`
- [ ] `GRAPH_TABLE`
- [ ] Graph pattern `MATCH`
- [ ] Path and quantified path patterns
- [ ] Graph labels and properties in SQL

## Direct SQL and modules

- [ ] Direct SQL statement list (`;` terminated scripts)
- [x] Optional trailing `;` on one statement
- [ ] SQL-client module language
- [ ] Embedded SQL host program syntax
- [ ] `DECLARE SECTION`

## Information schema (Part 11)

- [ ] `INFORMATION_SCHEMA` as ordinary identifiers (no extra syntax)
