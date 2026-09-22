# Data types → Predefined

Remaining `SQL-STANDARD.md` items under Data types → Predefined. Types only appear
in `CAST(x AS …)` and `x::…`. No `CREATE TABLE`.

Reuse `DataType`. Type names stay identifiers (plus the datetime/interval
keywords `ParseTypeName` already allows). Not XML/JSON functions, not `ARRAY`
subquery, not `MULTISET` constructors, not `CREATE TYPE`.

Today `ParseDataType` is: one name, optional `DOUBLE PRECISION` /
`CHAR VARYING` / `CHARACTER VARYING`, optional `(n)` / `(p, s)`, optional
`WITH TIME ZONE`. Single-word names (`INTEGER`, `VARCHAR`, `XML`) already
parse. Multi-word tails, collection suffixes, `ROW(…)`, `REF(…)`, dotted
names, and `MDARRAY` do not.

Seam: `Sql.Parse` via `ParsePrefixTests`. After Write/Edit, run `code_check`
on changed paths, then `dotnet test tests/QlParse.Tests`.

---

## Plan

Work one phase at a time. Tick that phase's `SQL-STANDARD.md` line before
starting the next. First test of the whole file: `cast(x as character large
object)` parses as `CastExpression`.

1. `NameTail` on `DataType` (unlocks every multi-word name).
2. Exact numeric — tests, tick.
3. Approximate numeric — tests, tick.
4. Character — `LARGE OBJECT`, tick.
5. National character — tests plus `NATIONAL …` tails, tick.
6. Binary — `BINARY VARYING` / `BINARY LARGE OBJECT`, tick.
7. `XML` / `JSON` — tests, tick.
8. `ARRAY` type and typed constructors.
9. `MULTISET`.
10. `ROW`.
11. `REF`.
12. User-defined distinct / structured types (dotted names).
13. `MD-ARRAY`.

---

## Shared

### `DataType`

Replace `SecondName` with `NameTail` (`IReadOnlyList<SyntaxToken>`, empty when
the name is one word). Update `Parses_cast_and_types`: `DOUBLE PRECISION` and
`CHAR VARYING` assert `NameTail` count 1.

Add only as a later phase needs them:

- `Collections` — `IReadOnlyList<CollectionSuffix>`
- `Fields` — `IReadOnlyList<FieldDefinition>?` (`ROW` only)
- `ReferencedType` / `ScopeKeyword` / `ScopeName` (`REF` only)

`CollectionSuffix`: `Keyword` (`ArrayKeyword` or `MULTISET`/`MDARRAY`
identifier), optional `[ cardinality ]` for `ARRAY` / `MDARRAY`. `MDARRAY`
dimensions that use `:` live on that suffix (phase 13).

`FieldDefinition`: identifier + nested `DataType`.

`VisitDataType` walks `Fields` and `ReferencedType`. Empty today.

### Parser

`ParseDataType` in `Parser.Prefix.cs`. After the first name, consume a known
tail (not an arbitrary identifier). Then the existing precision parens and
`WITH TIME ZONE`. Then zero or more collection suffixes.

Known tails (first name, case-insensitive via `TokenEquals` /
`IdentifierEquals`):

- `DOUBLE` → `PRECISION`
- `CHAR` / `CHARACTER` → `VARYING` or `LARGE` `OBJECT`
- `NATIONAL` → `CHAR` / `CHARACTER`, then optional `VARYING` or `LARGE` `OBJECT`
- `NCHAR` → `VARYING` or `LARGE` `OBJECT`
- `BINARY` → `VARYING` or `LARGE` `OBJECT`

Add `Keyword` string constants as each phase needs them. Do not add
`SyntaxKind` entries for type names. `ARRAY` is already a keyword; use that
for the suffix. `ROW`, `REF`, `MULTISET`, `MDARRAY` stay identifiers.

`ParseTypeName` still rejects `ArrayKeyword`. `array[1]` stays the
constructor.

### Tests and sample

Extend `Parses_cast_and_types` (or add a fact next to it). Cover both `CAST`
and `::` once per production, not every synonym on both. One new fragment in
`sample.sql` per ticked checkbox. `Parses_sample_sql` only checks parse
success.

---

## Instructions

Work one failing test at a time.

### 1. `NameTail`

`src/QlParse/SyntaxNodes.cs`: drop `SecondName`, add `NameTail`.
`Parser.Prefix.cs`: write `PRECISION` / `VARYING` into `NameTail`.
`ParsePrefixTests`: `Assert.Equal(Count.One, dbl.NameTail.Count)` (add
`Count.One = 1` if `code_check` rejects a raw `1`).

Done when `cast(x as double precision)` and `cast(x as char varying(10))`
still parse and no `SecondName` remains.

### 2. Exact numeric

Already accepted as identifiers with optional `(p, s)`.

Tests: `NUMERIC`, `DECIMAL`, `SMALLINT`, `INTEGER`, `BIGINT`, plus `INT` and
`DEC`. `NUMERIC(10, 2)` already exists. `SMALLINT` has no parens.

Tick `Exact numeric`.

### 3. Approximate numeric

`DOUBLE PRECISION` already has a line. Tests: `FLOAT`, `FLOAT(53)`, `REAL`.

Tick `Approximate numeric`.

### 4. Character

`CHAR` / `VARCHAR` / `CLOB` already parse. Implement `CHARACTER LARGE OBJECT`
and `CHAR LARGE OBJECT` via `NameTail`.

Tests: `CHAR(10)`, `VARCHAR(10)`, `CLOB`, `CHARACTER LARGE OBJECT`,
`CHAR LARGE OBJECT(10)`.

Not `CHARACTER SET` / `COLLATE` on the type (expression `COLLATE` already
exists). Not `CHARACTERS` / `OCTETS` length units. Not `K`/`M`/`G`
multipliers.

Tick `Character`.

### 5. National character

`NCHAR` / `NVARCHAR` / `NCLOB` already parse. Implement `NATIONAL CHARACTER`
/ `NATIONAL CHAR` / `NATIONAL CHARACTER VARYING` / `NCHAR VARYING` /
`NATIONAL CHARACTER LARGE OBJECT`.

Tests: `NCHAR(10)`, `NVARCHAR(10)`, `NCLOB`, `NATIONAL CHARACTER VARYING(10)`.

Tick `National character`.

### 6. Binary

`BINARY` / `VARBINARY` / `BLOB` already parse. Implement `BINARY VARYING` and
`BINARY LARGE OBJECT`.

Tests: `BINARY(8)`, `VARBINARY(8)`, `BLOB`, `BINARY VARYING(8)`,
`BINARY LARGE OBJECT`.

Tick `Binary`.

### 7. `XML` and `JSON`

Bare names, already accepted. Tests: `cast(x as xml)`, `cast(x as json)`,
`x::xml`.

Not `XML(DOCUMENT|CONTENT|SEQUENCE)` (SQL/XML section). Not JSON functions.

Tick `XML` and `JSON`.

### 8. `ARRAY` type and typed constructors

After the base type, if `_current` is `ArrayKeyword`, consume it. Optional
`[ number ]`. Repeat for nested `INTEGER ARRAY ARRAY`.

Typed constructor = existing `ARRAY[…]` value with this type on `CAST` / `::`.

Tests:

- `cast(x as integer array)` — `Collections` count 1, keyword `ArrayKeyword`,
  no brackets
- `cast(x as integer array[3])` — cardinality 3
- `cast(array[1, 2] as integer array)`
- `array[1]::integer array`

Not `INTEGER[]`. Not `ARRAY(SELECT …)`.

`VisitDataType` unchanged (no nested `DataType`). Tick `ARRAY type and typed
constructors`.

### 9. `MULTISET`

Same suffix path as `ARRAY`, identifier `MULTISET`, no brackets.

Tests: `cast(x as integer multiset)`, `cast(x as integer array multiset)`.

Not `MULTISET[…]` constructors (Collections section).

Tick `MULTISET`.

### 10. `ROW`

Before `ParseTypeName`: if `IdentifierEquals(Keyword.Row)` and next is `(`,
parse `ROW ( field type [, …] )`. Field = identifier + `ParseDataType()`.
Then collection suffixes.

`(` after `ROW` is a field list, not precision. `cast(x as row(1))` errors
(expected field name).

Tests: `cast(x as row(a int, b varchar(10)))` — two fields, nested types;
`cast(x as row(a int) array)`.

`VisitDataType` visits each field's `Type`. Tick `ROW`.

### 11. `REF`

If `IdentifierEquals(Keyword.Ref)` and next is `(`, parse
`REF ( type )` then optional `SCOPE` identifier (or dotted table name, same
loop as `ParseTableReference`).

Tests: `cast(x as ref(foo))`, `cast(x as ref(foo) scope t)`.

`VisitDataType` visits `ReferencedType`. Tick `REF`.

### 12. User-defined distinct / structured types

Unqualified `my_udt` already parses. After `ParseTypeName`, while `.`,
consume dotted identifiers into `NameTail` (or a dedicated `NameParts` list
if mixing `CHARACTER LARGE OBJECT` tokens with dots is messy — dotted names
have no `LARGE`/`OBJECT` tail; keep them as `Name` plus extra name tokens
separated by dots you already skip, same as tables).

Do not treat `DOUBLE PRECISION` as dotted. Dots only when `_current` is
`Dot`.

Tests: `cast(x as my_udt)`, `cast(x as public.my_udt)`,
`cast(x as catalog.schema.my_udt)`.

Not `CREATE TYPE`. Not `NEW udt(…)`. Tick `User-defined distinct / structured
types`.

### 13. `MD-ARRAY`

Suffix identifier `MDARRAY` (SQL/MDA spelling; the checklist says `MD-ARRAY`).
Then `[` dimensions `]`. A dimension is `number` or `number : number`,
comma-separated.

Tests: `cast(x as integer mdarray[5])`, `cast(x as integer mdarray[0:4, 0:9])`.

Not slicing / MDA ops (SQL/MDA section). Tick `MD-ARRAY`.

### Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `cast(x as integer array)` fails because `ARRAY` is a keyword in
`ParseTypeName`, the suffix must run after the base name, not as the name.
If `array[1]` becomes a type, prefix `ArrayKeyword` still goes to
`ParseArray`.
