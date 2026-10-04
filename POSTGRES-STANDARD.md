# PostgreSQL dialect coverage (9.x–18)

ISO core: SQL-STANDARD.md. This file tracks PostgreSQL-specific syntax beyond/diverging from ISO SQL, for use under the `SqlOptions.Postgres` flag. `[ ]` = not yet implemented.

## Lexical

### Strings and quoting
- [x] Dollar-quoted strings (`$$...$$`)
- [x] Tagged dollar-quoted strings (`$tag$...$tag$`)
- [x] C-style escape strings (`E'...'`)
- [x] `standard_conforming_strings` backslash-escape behavior (plain `''` strings never interpret backslashes, matching modern Postgres' only supported mode)
- [x] Unicode code point escapes in `E''` strings (`\uXXXX` `\UXXXXXXXX`)
- [x] `UESCAPE` clause on Unicode-escaped strings/identifiers

### Operators and parameters
- [ ] `::` cast operator
- [ ] Positional parameters (`$1` `$2` …)
- [ ] Named function argument notation (`=>`)
- [ ] Array subscript and slice syntax (`arr[2]` `arr[1:3]`)
- [ ] Custom/user-defined operator tokens (arbitrary operator-character sequences)

## Data types
- [ ] `SERIAL` `SMALLSERIAL` `BIGSERIAL` (implicit sequence + default)
- [ ] Array types (`int[]`, `text[]`, multi-dimensional `int[][]`)
- [ ] `JSON` / `JSONB`
- [ ] `HSTORE` (contrib extension)
- [ ] Geometric types (`POINT` `LINE` `LSEG` `BOX` `PATH` `POLYGON` `CIRCLE`)
- [ ] Network address types (`CIDR` `INET` `MACADDR` `MACADDR8`)
- [ ] Range types (`INT4RANGE` `INT8RANGE` `NUMRANGE` `TSRANGE` `TSTZRANGE` `DATERANGE`)
- [ ] Multirange types (`INT4MULTIRANGE` `TSMULTIRANGE` etc., PG14+)
- [ ] `UUID`
- [ ] `MONEY`
- [ ] Composite/row types via `CREATE TYPE ... AS (...)`
- [ ] Enumerated types via `CREATE TYPE ... AS ENUM (...)`
- [ ] Full text search types (`TSVECTOR` `TSQUERY`)
- [ ] Pseudo-types (`ANYELEMENT` `ANYARRAY` `ANYRANGE` `ANYCOMPATIBLE` `VOID` `RECORD` `CSTRING` `INTERNAL` `TRIGGER`)
- [ ] Object identifier / reg-types (`OID` `REGCLASS` `REGPROC` `REGTYPE` `REGNAMESPACE` `REGROLE`)
- [ ] `PG_LSN`
- [ ] `TXID_SNAPSHOT` / `PG_SNAPSHOT`

## Operators
- [ ] Array operators (`@>` `<@` `&&` `||` concatenation)
- [ ] JSON/JSONB operators (`->` `->>` `#>` `#>>` `@>` `<@` `?` `?|` `?&` `-` `#-` `@?` `@@`)
- [ ] Regular expression match operators (`~` `~*` `!~` `!~*`)
- [ ] `ILIKE` / `NOT ILIKE`
- [ ] Range operators (`@>` `<@` `&&` `<<` `>>` `&<` `&>` `-|-`)
- [ ] Network/inet operators (`<<` `<<=` `>>` `>>=` `&&` `~` `&` `|`)
- [ ] Geometric operators (`<->` `&&` `<<` `>>` `<^` `>^` `?#` `@>` `<@` `~=`)
- [ ] `CREATE OPERATOR` custom infix/prefix operator definitions

## Functions

### Array
- [ ] `unnest()`
- [ ] `array_agg()`
- [ ] `array_length()` `array_upper()` `array_lower()` `array_ndims()`
- [ ] `array_append()` `array_prepend()` `array_cat()` `array_remove()` `array_replace()`
- [ ] `array_to_string()` / `string_to_array()`
- [ ] `array_position()` / `array_positions()`
- [ ] `generate_subscripts()`

### JSON / JSONB
- [ ] `jsonb_build_object()` `jsonb_build_array()` `json_build_object()` `json_build_array()`
- [ ] `json_agg()` `jsonb_agg()` `json_object_agg()` `jsonb_object_agg()`
- [ ] `jsonb_set()` `jsonb_set_lax()` `jsonb_insert()`
- [ ] `jsonb_strip_nulls()` / `json_strip_nulls()`
- [ ] `jsonb_pretty()`
- [ ] `jsonb_path_query()` `jsonb_path_exists()` `jsonb_path_match()` (`jsonpath` type)
- [ ] `json_to_record()` / `jsonb_to_record()` / `*_to_recordset()`
- [ ] `jsonb_populate_record()` / `jsonb_populate_recordset()`
- [ ] `row_to_json()` `to_json()` `to_jsonb()`
- [ ] `jsonb_each()` `jsonb_each_text()` `jsonb_array_elements()` `jsonb_array_elements_text()`

### String
- [ ] `format()`
- [ ] `concat()` / `concat_ws()`
- [ ] `split_part()`
- [ ] `regexp_matches()` `regexp_replace()` `regexp_split_to_array()` `regexp_split_to_table()` `regexp_count()`
- [ ] `to_char()` `to_number()` `to_date()` `to_timestamp()`
- [ ] `quote_ident()` `quote_literal()` `quote_nullable()`
- [ ] `left()` `right()` `lpad()` `rpad()`
- [ ] `starts_with()`

### Date / time
- [ ] `now()` `clock_timestamp()` `statement_timestamp()` `transaction_timestamp()`
- [ ] `age()`
- [ ] `date_trunc()` / `date_part()`
- [ ] `make_date()` `make_time()` `make_timestamp()` `make_timestamptz()` `make_interval()`
- [ ] `isfinite()`

### Set-returning / table functions
- [ ] `generate_series()`
- [ ] `WITH ORDINALITY` row-number column for set-returning functions in `FROM`
- [ ] `ROWS FROM (...)` combining multiple function calls column-wise

### Aggregates
- [ ] `string_agg()`
- [ ] `array_agg()` / `jsonb_agg()` / `jsonb_object_agg()` as built-ins
- [ ] `CREATE AGGREGATE` custom aggregate definitions

### System and introspection
- [ ] `pg_typeof()`
- [ ] `current_setting()` / `set_config()`
- [ ] `nextval()` `currval()` `setval()` `lastval()` (function-call sequence access)
- [ ] `txid_current()` / `pg_current_xact_id()`

## Query expressions
- [ ] `DISTINCT ON (expression [, ...])`
- [ ] `LIMIT [count | ALL]`
- [ ] `OFFSET start` (Postgres-native form alongside ISO `FETCH FIRST`)
- [ ] `WITH query_name AS (...) [MATERIALIZED|NOT MATERIALIZED]`
- [ ] `TABLESAMPLE {BERNOULLI|SYSTEM} (pct) [REPEATABLE (seed)]`
- [ ] Implicit `LATERAL` for set-returning functions in `FROM` without the keyword
- [ ] `TABLE table_name` shorthand for `SELECT * FROM table_name`

## DML
- [ ] `RETURNING` clause on `INSERT` / `UPDATE` / `DELETE` / `MERGE`
- [ ] `INSERT ... ON CONFLICT (...) DO NOTHING`
- [ ] `INSERT ... ON CONFLICT (...) DO UPDATE SET ...`
- [ ] `ON CONFLICT ON CONSTRAINT constraint_name`
- [ ] `EXCLUDED` pseudo-table in `ON CONFLICT DO UPDATE`
- [ ] `UPDATE ... FROM`
- [ ] `DELETE ... USING`
- [ ] Writable CTEs chaining DML (`WITH t AS (INSERT/UPDATE/DELETE ... RETURNING ...) ...`)
- [ ] `COPY table_name FROM/TO {filename|PROGRAM command|STDIN|STDOUT} [WITH (...)]`

## Schema / DDL

### Tables and inheritance
- [ ] `CREATE TABLE ... INHERITS (...)`
- [ ] `CREATE UNLOGGED TABLE`
- [ ] `CREATE TABLE ... WITH (storage_parameter = value, ...)`
- [ ] `CREATE TABLE ... USING method` (table access methods)
- [ ] `ALTER TABLE ... INHERIT` / `NO INHERIT`
- [ ] `ALTER TABLE ... SET LOGGED` / `SET UNLOGGED`
- [ ] `ALTER TABLE ... SET STATISTICS`
- [ ] `ALTER TABLE ... SET (storage parameters)`
- [ ] `ALTER TABLE ... CLUSTER ON` / `SET WITHOUT CLUSTER`
- [ ] `ALTER COLUMN ... SET STORAGE {PLAIN|EXTERNAL|EXTENDED|MAIN}`
- [ ] `ALTER COLUMN ... SET DATA TYPE ... USING expression`
- [ ] `ONLY` to exclude inheriting child tables in DML/DDL
- [ ] `GENERATED ALWAYS AS (expr) STORED` computed columns

### Partitioning
- [ ] `PARTITION BY RANGE (...)`
- [ ] `PARTITION BY LIST (...)`
- [ ] `PARTITION BY HASH (...)`
- [ ] `CREATE TABLE ... PARTITION OF parent FOR VALUES {IN (...)|FROM (...) TO (...)|WITH (MODULUS, REMAINDER)}`
- [ ] `PARTITION ... DEFAULT`
- [ ] `ALTER TABLE ... ATTACH PARTITION ... FOR VALUES ...`
- [ ] `ALTER TABLE ... DETACH PARTITION ... [CONCURRENTLY]`

### Indexes
- [ ] `CREATE INDEX ... USING {btree|hash|gist|gin|brin|spgist}`
- [ ] `CREATE INDEX CONCURRENTLY`
- [ ] Partial indexes (`CREATE INDEX ... WHERE condition`)
- [ ] Expression indexes
- [ ] `CREATE INDEX ... INCLUDE (...)`
- [ ] Operator class specification (`col jsonb_path_ops`, `col gin_trgm_ops`, etc.)
- [ ] `REINDEX [TABLE|INDEX|SCHEMA|DATABASE|SYSTEM] [CONCURRENTLY]`

### Types, domains, and enums
- [ ] `CREATE TYPE name AS (...)` composite
- [ ] `CREATE TYPE name AS ENUM (...)`
- [ ] `CREATE TYPE name AS RANGE (SUBTYPE = ..., ...)`
- [ ] `CREATE TYPE name` shell type / `CREATE TYPE name (INPUT=, OUTPUT=, ...)` base type
- [ ] `ALTER TYPE ... ADD VALUE [IF NOT EXISTS] 'v' [BEFORE|AFTER 'x']`
- [ ] `ALTER TYPE ... RENAME VALUE 'old' TO 'new'`

### Views and materialized views
- [ ] `CREATE OR REPLACE VIEW`
- [ ] `CREATE MATERIALIZED VIEW`
- [ ] `REFRESH MATERIALIZED VIEW [CONCURRENTLY]`
- [ ] `CREATE RULE`

### Sequences and identity
- [ ] `CREATE SEQUENCE ... AS data_type`
- [ ] `ALTER SEQUENCE ... OWNED BY`

### Tablespaces and storage
- [ ] `CREATE TABLESPACE name LOCATION 'path'`
- [ ] `DROP TABLESPACE`
- [ ] `TABLESPACE` clause on `CREATE TABLE` / `CREATE INDEX`

### Extensions and languages
- [ ] `CREATE EXTENSION [IF NOT EXISTS] name [VERSION ...] [SCHEMA ...]`
- [ ] `ALTER EXTENSION ... UPDATE` / `ADD` / `DROP`
- [ ] `DROP EXTENSION`
- [ ] `CREATE [OR REPLACE] [TRUSTED] [PROCEDURAL] LANGUAGE`

### Foreign data
- [ ] `CREATE FOREIGN DATA WRAPPER`
- [ ] `CREATE SERVER`
- [ ] `CREATE USER MAPPING FOR ... SERVER ...`
- [ ] `CREATE FOREIGN TABLE`
- [ ] `IMPORT FOREIGN SCHEMA`

### Operator, aggregate, and cast definitions
- [ ] `CREATE OPERATOR`
- [ ] `CREATE OPERATOR CLASS` / `CREATE OPERATOR FAMILY`
- [ ] `CREATE AGGREGATE`
- [ ] `CREATE CAST ... WITH FUNCTION ... AS {IMPLICIT|ASSIGNMENT}`
- [ ] `CREATE COLLATION ... (PROVIDER = ...)`

## Constraints
- [ ] Exclusion constraints (`EXCLUDE USING index_method (column WITH operator, ...)`)
- [ ] `ALTER TABLE ... ADD CONSTRAINT ... NOT VALID`
- [ ] `ALTER TABLE ... VALIDATE CONSTRAINT constraint_name`

## Triggers and event triggers
- [ ] `CREATE TRIGGER ... FOR EACH ROW|STATEMENT`
- [ ] `CREATE TRIGGER ... WHEN (condition)`
- [ ] `CREATE TRIGGER ... REFERENCING OLD TABLE AS ... NEW TABLE AS ...` (transition tables)
- [ ] `CREATE CONSTRAINT TRIGGER`
- [ ] `CREATE EVENT TRIGGER name ON event WHEN ... EXECUTE FUNCTION ...`
- [ ] Trigger function special variables (`NEW` `OLD` `TG_OP` `TG_TABLE_NAME` `TG_WHEN` `TG_ARGV`)
- [ ] `EXECUTE FUNCTION` vs deprecated `EXECUTE PROCEDURE` on `CREATE TRIGGER`

## Procedural language (PL/pgSQL)
- [ ] `DO [LANGUAGE lang] $$ ... $$` anonymous code blocks
- [ ] `CREATE [OR REPLACE] FUNCTION ... LANGUAGE plpgsql`
- [ ] `CREATE [OR REPLACE] PROCEDURE ... LANGUAGE plpgsql`
- [ ] Function attributes: `IMMUTABLE` `STABLE` `VOLATILE` `STRICT` `PARALLEL {SAFE|RESTRICTED|UNSAFE}` `COST` `ROWS` `SECURITY DEFINER|INVOKER` `LEAKPROOF` `SET config_param`
- [ ] `RETURNS SETOF type` / `RETURNS TABLE (...)`
- [ ] `RAISE [DEBUG|LOG|INFO|NOTICE|WARNING|EXCEPTION] 'msg', args USING option = expr`
- [ ] `PERFORM query`
- [ ] `%TYPE` / `%ROWTYPE` attribute references
- [ ] `FOREACH var [SLICE n] IN ARRAY expr LOOP`
- [ ] `EXECUTE 'sql' INTO var USING params` (dynamic SQL in PL/pgSQL)
- [ ] `ASSERT condition [, 'message']`
- [ ] `GET DIAGNOSTICS var = {ROW_COUNT|PG_CONTEXT|...}` (PL/pgSQL item names)
- [ ] `REFCURSOR` variables and `FOR row IN query LOOP`

## Privileges and roles
- [ ] `CREATE ROLE ... WITH LOGIN|SUPERUSER|CREATEDB|CREATEROLE|REPLICATION|BYPASSRLS|CONNECTION LIMIT n|PASSWORD|VALID UNTIL`
- [ ] `ALTER DEFAULT PRIVILEGES [FOR ROLE ...] IN SCHEMA ... GRANT/REVOKE ...`
- [ ] `GRANT ... ON ALL TABLES IN SCHEMA ...`
- [ ] `REASSIGN OWNED BY old TO new`
- [ ] `DROP OWNED BY role`

## Row-level security
- [ ] `CREATE POLICY name ON table [AS {PERMISSIVE|RESTRICTIVE}] [FOR {ALL|SELECT|INSERT|UPDATE|DELETE}] [TO role] [USING (expr)] [WITH CHECK (expr)]`
- [ ] `ALTER POLICY` / `DROP POLICY`
- [ ] `ALTER TABLE ... ENABLE|DISABLE ROW LEVEL SECURITY`
- [ ] `ALTER TABLE ... FORCE|NO FORCE ROW LEVEL SECURITY`
- [ ] `SECURITY LABEL`

## Transactions, sessions, and configuration
- [ ] `BEGIN [WORK|TRANSACTION]` as alias for `START TRANSACTION`
- [ ] `SET parameter = value` / `SET LOCAL`
- [ ] `SHOW parameter` / `SHOW ALL`
- [ ] `RESET parameter` / `RESET ALL`
- [ ] `PREPARE TRANSACTION 'gid'` / `COMMIT PREPARED` / `ROLLBACK PREPARED` (two-phase commit)
- [ ] `LOCK [TABLE] name IN lock_mode MODE [NOWAIT]`

## Administration and utility commands
- [ ] `EXPLAIN [ANALYZE] [VERBOSE] [COSTS] [BUFFERS] [FORMAT {TEXT|XML|JSON|YAML}] statement`
- [ ] `VACUUM [FULL] [FREEZE] [VERBOSE] [ANALYZE] [table ...]`
- [ ] `ANALYZE [VERBOSE] [table ...]`
- [ ] `CLUSTER [table [USING index]]`
- [ ] `CHECKPOINT`
- [ ] `LISTEN channel` / `NOTIFY channel [, 'payload']` / `UNLISTEN`
- [ ] `LOAD 'filename'`
- [ ] `DISCARD {ALL|PLANS|SEQUENCES|TEMP}`
- [ ] `ALTER SYSTEM SET parameter = value`

## Full text search
- [ ] `@@` tsvector/tsquery match operator
- [ ] `to_tsvector()` `to_tsquery()` `plainto_tsquery()` `phraseto_tsquery()` `websearch_to_tsquery()`
- [ ] `ts_rank()` / `ts_rank_cd()`
- [ ] `ts_headline()`
- [ ] `setweight()`
- [ ] tsquery combinators (`&&` AND, `||` OR, `!!` NOT, `<->` FOLLOWED BY)
- [ ] `CREATE TEXT SEARCH CONFIGURATION`
- [ ] `CREATE TEXT SEARCH DICTIONARY`
- [ ] `CREATE TEXT SEARCH PARSER`
- [ ] `CREATE TEXT SEARCH TEMPLATE`

## Replication
- [ ] `CREATE PUBLICATION name FOR TABLE ... | FOR ALL TABLES`
- [ ] `ALTER PUBLICATION` / `DROP PUBLICATION`
- [ ] `CREATE SUBSCRIPTION name CONNECTION '...' PUBLICATION ...`
- [ ] `ALTER SUBSCRIPTION` / `DROP SUBSCRIPTION`

## System columns and catalog access
- [ ] System columns (`ctid` `xmin` `xmax` `cmin` `cmax` `tableoid`, legacy `oid`)
- [ ] `pg_catalog` implicit schema resolution
- [ ] Object-identifier cast shorthand (`'name'::regclass`, `'t'::regtype`)
