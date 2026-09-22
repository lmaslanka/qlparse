# Approximate numeric (exponent)

SQL-92: `<mantissa> E <exponent>` where mantissa is an exact numeric and
exponent is a signed integer. `1E2`, `1.5e+10`, `1.5E-3`. Same token as
integer/decimal.

Reuse `SyntaxKind.Number`. No new AST node. Do not evaluate the value. Not
`0x`/`0b`/`0o`. Not digit separators (`1_000`). Not leading-dot `.5e10` as one
token (today `.5` is already Dot then Number).

Today `ReadNumber` stops after digits and an optional fraction. `1e10` is
Number `1` then Identifier `e10`. `select 1e10` parses as literal `1` with
alias `e10`.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: after the integer/fraction in `ReadNumber`, if `E`/`e`, optional
   `+`/`-`, and at least one digit, consume that as part of the same Number.
2. Parser: no change. `LiteralExpression` already accepts Number.
3. Tests, then `sample.sql`, then tick SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `1e2` lexes as
`[Number, EndOfFile]`.

### 1. `src/QlParse/Lexer.cs`

`ReadNumber` already reads unsigned digits and, if `.` is followed by a digit,
the fraction. After that, try an exponent:

- next char is `E` or `e`
- then optional `+` or `-`
- then at least one ASCII digit

If those hold, consume the marker, sign, and all following digits. If `E`/`e`
is not followed by a digit (after an optional sign), do not consume the
marker: `1e` stays Number `1` then Identifier `e`; `1e+` stays Number `1`,
Identifier `e`, Plus.

Name consts for the exponent marker / sign characters if `code_check` flags
magic `'E'` / `'+'`. Do not use `TwoCharTokenLength` for the marker.

`1.` stays Number then Dot (`Trailing_dot_is_not_part_of_number`). Do not
make `1.e10` a single token. `.5e10` stays Dot then Number `5e10`.

Keep `SyntaxKind.Number`. Do not add a kind. Do not touch `Keyword.cs`.

### 2. Parser / AST

No change.

### 3. `tests/QlParse.Tests/LexTests.cs`

Next to `Numbers`:

- `1e2`, `1E2`, `1.5e+10`, `1.5E-3`, `1e0` are one Number
- `1e` is Number then Identifier
- `1e+` is Number, Identifier, Plus
- `1 e10` (space) is Number then Identifier

### 4. `tests/QlParse.Tests/ParsePrefixTests.cs` and `ParseSelectTests.cs`

- `1e10` is `LiteralExpression` with `SyntaxKind.Number`
- `select 1e10` has no alias (today it is `1` aliased `e10`)
- `select 1 e` still has alias `e`

### 5. `sample.sql`

Add one exponent literal in a place that already parses (select list or a
comparison). `Parses_sample_sql` only checks parse success.

### 6. `SQL-STANDARD.md`

Tick `Approximate numeric (exponent)`.

### 7. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `code_check` reports magic-literal, name the character as a const.
If `select 1 e` loses its alias, the exponent reader skipped trivia — only
look at the next source character, not after whitespace.
