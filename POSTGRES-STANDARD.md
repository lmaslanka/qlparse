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
- [x] `::` cast operator
- [x] Positional parameters (`$1` `$2` …)
- [x] Named function argument notation (`=>`)
- [x] Array subscript and slice syntax (`arr[2]` `arr[1:3]`)
- [x] Custom/user-defined operator tokens (arbitrary operator-character sequences)

## Data types
- [x] `SERIAL` `SMALLSERIAL` `BIGSERIAL` (implicit sequence + default)
- [x] Array types (`int[]`, `text[]`, multi-dimensional `int[][]`)
- [x] `JSON` / `JSONB`
- [x] `HSTORE` (contrib extension)
- [x] Geometric types (`POINT` `LINE` `LSEG` `BOX` `PATH` `POLYGON` `CIRCLE`)
- [x] Network address types (`CIDR` `INET` `MACADDR` `MACADDR8`)
- [x] Range types (`INT4RANGE` `INT8RANGE` `NUMRANGE` `TSRANGE` `TSTZRANGE` `DATERANGE`)
- [x] Multirange types (`INT4MULTIRANGE` `TSMULTIRANGE` etc., PG14+)
- [x] `UUID`
- [x] `MONEY`
- [x] Composite/row types via `CREATE TYPE ... AS (...)`
- [x] Enumerated types via `CREATE TYPE ... AS ENUM (...)`
- [x] Full text search types (`TSVECTOR` `TSQUERY`)
- [x] Pseudo-types (`ANYELEMENT` `ANYARRAY` `ANYRANGE` `ANYCOMPATIBLE` `VOID` `RECORD` `CSTRING` `INTERNAL` `TRIGGER`)
- [x] Object identifier / reg-types (`OID` `REGCLASS` `REGPROC` `REGTYPE` `REGNAMESPACE` `REGROLE`)
- [x] `PG_LSN`
- [x] `TXID_SNAPSHOT` / `PG_SNAPSHOT`

## Operators
- [x] Array operators (`@>` `<@` `&&` `||` concatenation)
- [x] JSON/JSONB operators (`->` `->>` `#>` `#>>` `@>` `<@` `?` `?|` `?&` `-` `#-` `@?` `@@`)
- [x] Regular expression match operators (`~` `~*` `!~` `!~*`)
- [x] `ILIKE` / `NOT ILIKE`
- [x] Range operators (`@>` `<@` `&&` `<<` `>>` `&<` `&>` `-|-`)
- [x] Network/inet operators (`<<` `<<=` `>>` `>>=` `&&` `~` `&` `|`)
- [x] Geometric operators (`<->` `&&` `<<` `>>` `<^` `>^` `?#` `@>` `<@` `~=`)
- [x] `CREATE OPERATOR` custom infix/prefix operator definitions

## Functions

### Array
- [x] `unnest()`
- [x] `array_agg()`
- [x] `array_length()` `array_upper()` `array_lower()` `array_ndims()`
- [x] `array_append()` `array_prepend()` `array_cat()` `array_remove()` `array_replace()`
- [x] `array_to_string()` / `string_to_array()`
- [x] `array_position()` / `array_positions()`
- [x] `generate_subscripts()`

### JSON / JSONB
- [x] `jsonb_build_object()` `jsonb_build_array()` `json_build_object()` `json_build_array()`
- [x] `json_agg()` `jsonb_agg()` `json_object_agg()` `jsonb_object_agg()`
- [x] `jsonb_set()` `jsonb_set_lax()` `jsonb_insert()`
- [x] `jsonb_strip_nulls()` / `json_strip_nulls()`
- [x] `jsonb_pretty()`
- [x] `jsonb_path_query()` `jsonb_path_exists()` `jsonb_path_match()` (`jsonpath` type)
- [x] `json_to_record()` / `jsonb_to_record()` / `*_to_recordset()`
- [x] `jsonb_populate_record()` / `jsonb_populate_recordset()`
- [x] `row_to_json()` `to_json()` `to_jsonb()`
- [x] `jsonb_each()` `jsonb_each_text()` `jsonb_array_elements()` `jsonb_array_elements_text()`

### String
- [x] `format()`
- [x] `concat()` / `concat_ws()`
- [x] `split_part()`
- [x] `regexp_matches()` `regexp_replace()` `regexp_split_to_array()` `regexp_split_to_table()` `regexp_count()`
- [x] `to_char()` `to_number()` `to_date()` `to_timestamp()`
- [x] `quote_ident()` `quote_literal()` `quote_nullable()`
- [x] `left()` `right()` `lpad()` `rpad()`
- [x] `starts_with()`

### Date / time
- [x] `now()` `clock_timestamp()` `statement_timestamp()` `transaction_timestamp()`
- [x] `age()`
- [x] `date_trunc()` / `date_part()`
- [x] `make_date()` `make_time()` `make_timestamp()` `make_timestamptz()` `make_interval()`
- [x] `isfinite()`

### Set-returning / table functions
- [x] `generate_series()`
- [ ] `WITH ORDINALITY` row-number column for set-returning functions in `FROM`
- [ ] `ROWS FROM (...)` combining multiple function calls column-wise

### Aggregates
- [x] `string_agg()`
- [x] `array_agg()` / `jsonb_agg()` / `jsonb_object_agg()` as built-ins
- [x] `CREATE AGGREGATE` custom aggregate definitions

### System and introspection
- [x] `pg_typeof()`
- [x] `current_setting()` / `set_config()`
- [x] `nextval()` `currval()` `setval()` `lastval()` (function-call sequence access)
- [x] `txid_current()` / `pg_current_xact_id()`

## Query expressions
- [ ] `DISTINCT ON (expression [, ...])`
- [ ] `LIMIT [count | ALL]`
- [x] `OFFSET start` (Postgres-native form alongside ISO `FETCH FIRST`)
- [ ] `WITH query_name AS (...) [MATERIALIZED|NOT MATERIALIZED]`
- [x] `TABLESAMPLE {BERNOULLI|SYSTEM} (pct) [REPEATABLE (seed)]`
- [x] Implicit `LATERAL` for set-returning functions in `FROM` without the keyword
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
- [x] `CREATE TABLE ... INHERITS (...)`
- [x] `CREATE UNLOGGED TABLE`
- [x] `CREATE TABLE ... WITH (storage_parameter = value, ...)`
- [x] `CREATE TABLE ... USING method` (table access methods)
- [x] `ALTER TABLE ... INHERIT` / `NO INHERIT`
- [x] `ALTER TABLE ... SET LOGGED` / `SET UNLOGGED`
- [x] `ALTER TABLE ... SET STATISTICS`
- [x] `ALTER TABLE ... SET (storage parameters)`
- [x] `ALTER TABLE ... CLUSTER ON` / `SET WITHOUT CLUSTER`
- [x] `ALTER COLUMN ... SET STORAGE {PLAIN|EXTERNAL|EXTENDED|MAIN}`
- [x] `ALTER COLUMN ... SET DATA TYPE ... USING expression`
- [ ] `ONLY` to exclude inheriting child tables in DML/DDL
- [x] `GENERATED ALWAYS AS (expr) STORED` computed columns

### Partitioning
- [x] `PARTITION BY RANGE (...)`
- [x] `PARTITION BY LIST (...)`
- [x] `PARTITION BY HASH (...)`
- [x] `CREATE TABLE ... PARTITION OF parent FOR VALUES {IN (...)|FROM (...) TO (...)|WITH (MODULUS, REMAINDER)}`
- [x] `PARTITION ... DEFAULT`
- [x] `ALTER TABLE ... ATTACH PARTITION ... FOR VALUES ...`
- [x] `ALTER TABLE ... DETACH PARTITION ... [CONCURRENTLY]`

### Indexes
- [x] `CREATE INDEX ... USING {btree|hash|gist|gin|brin|spgist}`
- [x] `CREATE INDEX CONCURRENTLY`
- [x] Partial indexes (`CREATE INDEX ... WHERE condition`)
- [x] Expression indexes
- [x] `CREATE INDEX ... INCLUDE (...)`
- [x] Operator class specification (`col jsonb_path_ops`, `col gin_trgm_ops`, etc.)
- [x] `REINDEX [TABLE|INDEX|SCHEMA|DATABASE|SYSTEM] [CONCURRENTLY]`

### Types, domains, and enums
- [x] `CREATE TYPE name AS (...)` composite
- [x] `CREATE TYPE name AS ENUM (...)`
- [x] `CREATE TYPE name AS RANGE (SUBTYPE = ..., ...)`
- [x] `CREATE TYPE name` shell type / `CREATE TYPE name (INPUT=, OUTPUT=, ...)` base type
- [x] `ALTER TYPE ... ADD VALUE [IF NOT EXISTS] 'v' [BEFORE|AFTER 'x']`
- [x] `ALTER TYPE ... RENAME VALUE 'old' TO 'new'`

### Views and materialized views
- [x] `CREATE OR REPLACE VIEW`
- [x] `CREATE MATERIALIZED VIEW`
- [x] `REFRESH MATERIALIZED VIEW [CONCURRENTLY]`
- [x] `CREATE RULE`

### Sequences and identity
- [x] `CREATE SEQUENCE ... AS data_type`
- [x] `ALTER SEQUENCE ... OWNED BY`

### Tablespaces and storage
- [x] `CREATE TABLESPACE name LOCATION 'path'`
- [x] `DROP TABLESPACE`
- [x] `TABLESPACE` clause on `CREATE TABLE` / `CREATE INDEX`

### Extensions and languages
- [x] `CREATE EXTENSION [IF NOT EXISTS] name [VERSION ...] [SCHEMA ...]`
- [x] `ALTER EXTENSION ... UPDATE` / `ADD` / `DROP`
- [x] `DROP EXTENSION`
- [x] `CREATE [OR REPLACE] [TRUSTED] [PROCEDURAL] LANGUAGE`

### Foreign data
- [x] `CREATE FOREIGN DATA WRAPPER`
- [x] `CREATE SERVER`
- [x] `CREATE USER MAPPING FOR ... SERVER ...`
- [x] `CREATE FOREIGN TABLE`
- [x] `IMPORT FOREIGN SCHEMA`

### Operator, aggregate, and cast definitions
- [x] `CREATE OPERATOR`
- [x] `CREATE OPERATOR CLASS` / `CREATE OPERATOR FAMILY`
- [x] `CREATE AGGREGATE`
- [x] `CREATE CAST ... WITH FUNCTION ... AS {IMPLICIT|ASSIGNMENT}`
- [x] `CREATE COLLATION ... (PROVIDER = ...)`

## Constraints
- [x] Exclusion constraints (`EXCLUDE USING index_method (column WITH operator, ...)`)
- [x] `ALTER TABLE ... ADD CONSTRAINT ... NOT VALID`
- [x] `ALTER TABLE ... VALIDATE CONSTRAINT constraint_name`

## Triggers and event triggers
- [x] `CREATE TRIGGER ... FOR EACH ROW|STATEMENT`
- [x] `CREATE TRIGGER ... WHEN (condition)`
- [x] `CREATE TRIGGER ... REFERENCING OLD TABLE AS ... NEW TABLE AS ...` (transition tables)
- [x] `CREATE CONSTRAINT TRIGGER`
- [x] `CREATE EVENT TRIGGER name ON event WHEN ... EXECUTE FUNCTION ...`
- [ ] Trigger function special variables (`NEW` `OLD` `TG_OP` `TG_TABLE_NAME` `TG_WHEN` `TG_ARGV`)
- [x] `EXECUTE FUNCTION` vs deprecated `EXECUTE PROCEDURE` on `CREATE TRIGGER`

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
- [x] `CREATE ROLE ... WITH LOGIN|SUPERUSER|CREATEDB|CREATEROLE|REPLICATION|BYPASSRLS|CONNECTION LIMIT n|PASSWORD|VALID UNTIL`
- [x] `ALTER DEFAULT PRIVILEGES [FOR ROLE ...] IN SCHEMA ... GRANT/REVOKE ...`
- [x] `GRANT ... ON ALL TABLES IN SCHEMA ...`
- [x] `REASSIGN OWNED BY old TO new`
- [x] `DROP OWNED BY role`

## Row-level security
- [x] `CREATE POLICY name ON table [AS {PERMISSIVE|RESTRICTIVE}] [FOR {ALL|SELECT|INSERT|UPDATE|DELETE}] [TO role] [USING (expr)] [WITH CHECK (expr)]`
- [x] `ALTER POLICY` / `DROP POLICY`
- [x] `ALTER TABLE ... ENABLE|DISABLE ROW LEVEL SECURITY`
- [x] `ALTER TABLE ... FORCE|NO FORCE ROW LEVEL SECURITY`
- [x] `SECURITY LABEL`

## Transactions, sessions, and configuration
- [x] `BEGIN [WORK|TRANSACTION]` as alias for `START TRANSACTION`
- [x] `SET parameter = value` / `SET LOCAL`
- [x] `SHOW parameter` / `SHOW ALL`
- [x] `RESET parameter` / `RESET ALL`
- [x] `PREPARE TRANSACTION 'gid'` / `COMMIT PREPARED` / `ROLLBACK PREPARED` (two-phase commit)
- [x] `LOCK [TABLE] name IN lock_mode MODE [NOWAIT]`

## Administration and utility commands
- [x] `EXPLAIN [ANALYZE] [VERBOSE] [COSTS] [BUFFERS] [FORMAT {TEXT|XML|JSON|YAML}] statement`
- [x] `VACUUM [FULL] [FREEZE] [VERBOSE] [ANALYZE] [table ...]`
- [x] `ANALYZE [VERBOSE] [table ...]`
- [x] `CLUSTER [table [USING index]]`
- [x] `CHECKPOINT`
- [x] `LISTEN channel` / `NOTIFY channel [, 'payload']` / `UNLISTEN`
- [x] `LOAD 'filename'`
- [x] `DISCARD {ALL|PLANS|SEQUENCES|TEMP}`
- [x] `ALTER SYSTEM SET parameter = value`

## Full text search
- [x] `@@` tsvector/tsquery match operator
- [x] `to_tsvector()` `to_tsquery()` `plainto_tsquery()` `phraseto_tsquery()` `websearch_to_tsquery()`
- [x] `ts_rank()` / `ts_rank_cd()`
- [x] `ts_headline()`
- [x] `setweight()`
- [x] tsquery combinators (`&&` AND, `||` OR, `!!` NOT, `<->` FOLLOWED BY)
- [x] `CREATE TEXT SEARCH CONFIGURATION`
- [x] `CREATE TEXT SEARCH DICTIONARY`
- [x] `CREATE TEXT SEARCH PARSER`
- [x] `CREATE TEXT SEARCH TEMPLATE`

## Replication
- [x] `CREATE PUBLICATION name FOR TABLE ... | FOR ALL TABLES`
- [x] `ALTER PUBLICATION` / `DROP PUBLICATION`
- [x] `CREATE SUBSCRIPTION name CONNECTION '...' PUBLICATION ...`
- [x] `ALTER SUBSCRIPTION` / `DROP SUBSCRIPTION`

## System columns and catalog access
- [x] System columns (`ctid` `xmin` `xmax` `cmin` `cmax` `tableoid`, legacy `oid`)
- [x] `pg_catalog` implicit schema resolution
- [x] Object-identifier cast shorthand (`'name'::regclass`, `'t'::regtype`)
