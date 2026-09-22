# Scalar expressions → Conditionals and null

Remaining `SQL-STANDARD.md` items: `NULLIF` as special form, `COALESCE` as
special form. `CASE` is done. `COALESCE` as function call stays ticked.

ISO: `NULLIF (V1, V2)` and `COALESCE (V1, V2 {, Vi}…)`. Special form means a
dedicated node, not `FunctionCallExpression`. No `FILTER`, no `*`, no
`DISTINCT`.

Today both are identifiers. `nullif(a, b)` and `coalesce(a, b)` go through
`ParseFunctionCall`. `sample.sql` already has `coalesce(o.job_title, …)` as a
call.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

Not `CASE`. Not `NVL` / `IFNULL`. Not rewriting `NULLIF` into `CASE`.

---

## Plan

Work one phase at a time. Tick that phase's line before starting the next.
First test: `nullif` lexes as `[NullIfKeyword, EndOfFile]`.

1. `NULLIF` as special form
2. `COALESCE` as special form

---

## Instructions

Work one failing test at a time.

### 1. `NULLIF`

Exactly two expressions. Keyword so it is not `ParseFunctionCall`.

#### 1a. `src/QlParse/Keyword.cs` and `Keyword.Classify.cs`

`Keyword.NullIf = "nullif"`. Length 6: `Keyword.Select.Length` bucket.

#### 1b. `src/QlParse/SyntaxKind.cs`

`NullIfKeyword` next to `CastKeyword` / `TreatKeyword`.

#### 1c. `src/QlParse/SyntaxNodes.cs`

`NullIfExpression`: `NullIfKeyword`, `OpenParen`, `First`, `Second`,
`CloseParen`, `Span`. Do not store the comma.

#### 1d. `src/QlParse/Parser.Prefix.cs`

`SyntaxKind.NullIfKeyword => ParseNullIf()` next to `CastKeyword`. Parse:
keyword, `(`, expression, comma, expression, `)`. Extra args fail on the
close paren.

#### 1e. `src/QlParse/SqlVisitor.cs`

Dispatch next to `TreatExpression`. Visit `First` and `Second`.

#### 1f. Tests

`LexTests`: `nullif` → `NullIfKeyword` next to `cast`.

`ParsePrefixTests` next to `Parses_treat`:

- `nullif(a, b)` is `NullIfExpression`
- `nullif(a)` errors
- `select nullif from t` errors (keyword)

#### 1g. `sample.sql` and tick

One `NULLIF(…, …)` in the select list. Tick `NULLIF` as special form.

### 2. `COALESCE` as special form

At least two expressions. Same keyword treatment as `NULLIF`. After this,
`coalesce(a, b)` is `CoalesceExpression`, not `FunctionCallExpression`. Leave
the function-call checkbox ticked.

#### 2a. Keywords

`Keyword.Coalesce = "coalesce"`. Length 8: `Keyword.Distinct.Length` bucket.

`CoalesceKeyword` next to `NullIfKeyword`.

#### 2b. AST

`CoalesceExpression`: `CoalesceKeyword`, `OpenParen`, `Arguments`
(`IReadOnlyList<Expression>`), `CloseParen`, `Span`.

#### 2c. Parser

`SyntaxKind.CoalesceKeyword => ParseCoalesce()`. Parse expression list inside
parens (reuse `ParseExpressionList`). If fewer than two arguments, error
(`Expected` / `COALESCE requires two arguments` — match nearby messages:
`Expected …, found …` is enough if you require a comma after the first
expression, same shape as `NULLIF`).

No `FILTER` after `)`. A following `FILTER` is leftover tokens, not part of
this node.

#### 2d. Visitor

Visit all `Arguments` (`VisitAll`).

#### 2e. Tests

`LexTests`: `coalesce` → `CoalesceKeyword`.

`ParsePrefixTests`:

- `coalesce(a, b)` is `CoalesceExpression`, arguments count 2
- `coalesce(a, b, c)` arguments count 3 (`Count.Two` already exists; add
  `Count.Three` only if you need it — it exists)
- `coalesce(a)` errors
- `select coalesce from t` errors

Existing `sample.sql` `coalesce(…)` must still parse; `VisitorTests` walks
`sample.sql` / dense queries — add the new node to the visitor switch or
`Parses_sample_sql` throws `Unknown expression`.

#### 2f. Tick

Tick `COALESCE` as special form. Do not untick `COALESCE` as function call.

### Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `nullif(a, b)` is still `FunctionCallExpression`, it did not become a
keyword or `ParsePrefix` still sends `Identifier` + `(` to
`ParseFunctionCall`. If `Parses_sample_sql` throws `Unknown expression`, the
visitor switch missed `CoalesceExpression`.
