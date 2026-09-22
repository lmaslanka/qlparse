# Data types → Type operations

Remaining `SQL-STANDARD.md` items under Data types → Type operations: `TREAT`
and `NEXT VALUE FOR`. `CAST` is done. Expression forms only. No `CREATE
SEQUENCE`, no identity columns.

`TREAT (x AS type)` is the UDT subtype downcast (SQL:1999). Same shape as
`CAST`. `NEXT VALUE FOR seq` is the sequence generator value (SQL:2003).

Today `treat(x as int)` is a function call (`Identifier` + `(`). `next value
for seq` is identifier `next` then unexpected tokens (or a column alias
`value` in a select list).

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

Not `PREVIOUS VALUE FOR`. Not `CREATE SEQUENCE` (Sequences section). Do not
tick the duplicate `NEXT VALUE FOR` under Sequences and identity.

---

## Plan

Work one phase at a time. Tick that phase's line before starting the next.
First test: `treat` lexes as `[TreatKeyword, EndOfFile]`.

1. `TREAT`
2. `NEXT VALUE FOR`

---

## Instructions

Work one failing test at a time.

### 1. `TREAT`

Mirror `CAST`. New `TreatExpression` (do not reuse `CastExpression`). `TREAT`
is a keyword so `treat(x as int)` is not `ParseFunctionCall`.

#### 1a. `src/QlParse/Keyword.cs` and `Keyword.Classify.cs`

`Keyword.Treat = "treat"`. Length 5: add it in the `Keyword.Where.Length`
bucket in `Classify`, next to `Array` / `Match`.

#### 1b. `src/QlParse/SyntaxKind.cs`

`TreatKeyword` next to `CastKeyword`.

#### 1c. `src/QlParse/SyntaxNodes.cs`

Copy `CastExpression` as `TreatExpression` (`TreatKeyword` instead of
`CastKeyword`). Same other properties. Reuse `DataType`.

#### 1d. `src/QlParse/Parser.Prefix.cs`

`SyntaxKind.TreatKeyword => ParseTreat()` next to `CastKeyword`. `ParseTreat`
is `ParseCast` with `TreatExpression` / `TreatKeyword`.

#### 1e. `src/QlParse/SqlVisitor.cs`

Dispatch `TreatExpression` next to `CastExpression`. `VisitTreatExpression`
visits the expression and `VisitDataType` (same as cast).

#### 1f. Tests

`LexTests`: `treat` → `TreatKeyword` next to `cast`.

`ParsePrefixTests` next to `Parses_cast_and_types`:

- `treat(x as int)` is `TreatExpression`
- `treat(x as public.my_udt)` uses the existing dotted `DataType`

`select treat from t` must fail (keyword, like `cast`). Do not keep `treat` as
an identifier.

#### 1g. `sample.sql` and tick

One `TREAT(… AS …)` in the select list. Tick `TREAT`.

### 2. `NEXT VALUE FOR`

`<next value expression> ::= NEXT VALUE FOR <sequence generator name>`.
Name is dotted identifiers, same loop as `ParseDottedNameTail` / table names.

`NEXT` and `VALUE` stay identifiers so `select next from t` still parses.
`FOR` is already `ForKeyword`. Detect the three-word form in the `Identifier`
arm of `ParsePrefix`.

#### 2a. `src/QlParse/Keyword.cs`

`Keyword.Next = "next"` and `Keyword.Value = "value"`. Do not add
`SyntaxKind` entries. Do not touch `Classify`.

#### 2b. `src/QlParse/SyntaxNodes.cs`

`NextValueExpression`: `NextKeyword`, `ValueKeyword`, `ForKeyword`,
`NameParts` (`IReadOnlyList<SyntaxToken>`), `Span`.

#### 2c. `src/QlParse/Parser.cs` / `Parser.Prefix.cs`

You need two tokens of lookahead (`value` then `FOR`). `NextKind` is one.
Look at `_tokens[_index]` and `_tokens[_index + 1]` (same indexing as
`NextKind`: `_current` is already consumed from `_index - 1`). Guard
`_index + 1 < _tokens.Count`.

In `ParsePrefix`, `SyntaxKind.Identifier` arm: if current is `NEXT` and next
is identifier `VALUE` and the token after that is `ForKeyword`,
`ParseNextValue()`; else today's function-call / identifier split.

`ParseNextValue`: advance `NEXT`, expect identifier `VALUE` (`TokenEquals`),
expect `ForKeyword`, then one or more identifiers separated by `.` (skip the
dots, same as `ParseDottedNameTail`). At least one name token.

#### 2d. `src/QlParse/SqlVisitor.cs`

Dispatch `NextValueExpression`. Visit method empty (no nested expression).

#### 2e. Tests

`ParsePrefixTests`:

- `next value for seq` is `NextValueExpression`, `NameParts` count 1
- `next value for public.seq` — `NameParts` count 2
- `select next from t` still has identifier `next` (not this form)

`LexTests`: `next` and `value` stay `Identifier`.

#### 2f. `sample.sql` and tick

One `NEXT VALUE FOR …` in the select list. Tick Type operations
`NEXT VALUE FOR` only.

### Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `treat(x as int)` is still a function call, `TREAT` did not become a
keyword or `ParsePrefix` still sends `Identifier` + `(` to
`ParseFunctionCall`. If `select next from t` breaks, the lookahead consumed
`NEXT` without requiring `VALUE` + `FOR`.
