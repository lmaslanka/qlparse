# JSON `->` / `->>`

Postgres JSON extract: `metadata->'a'` (json) and `metadata->>'b'` (text).
Chained: `l.metadata->'billing_address'->>'country_code'` means
`(l.metadata->'billing_address')->>'country_code'`.

Reuse `BinaryExpression`. No new AST node. Not `#>` / `#>>`, not `@>`, not `jsonb_extract_path`.

Today `-` is always minus and `>` is always greater-than, so `->` is two tokens
and parses as subtraction then comparison.

Seam: `Sql.Format` via `FormatTests`. After Write/Edit, run `code_check` on
changed paths, then `dotnet test tests/SqlParser.Tests`.

---

## Plan

1. Lexer: one token for `->`, one for `->>`. Longest match first (`->>` before `->`).
2. Parser: same binding-power path as `||` / `+`, but tighter than `.` so
   `l.metadata->'a'` is `(l.metadata)->'a'`.
3. Formatter: no spaces (`metadata->'a'->>'b'`).
4. Dumper: already handles `BinaryExpression` — no change.
5. Tests, then `sample.sql`.

---

## Instructions

Work one failing test at a time. First test: `select metadata->'a' from t`
formats as `SELECT` / `metadata->'a'` / `FROM t`.

### 1. `src/SqlParser/SyntaxKind.cs`

Add two kinds next to `ConcatToken` / `DoubleColonToken`:
- `JsonArrowToken` for `->`
- `JsonTextArrowToken` for `->>`

Do not add keywords. Do not touch `Keyword.cs`.

### 2. `src/SqlParser/Lexer.cs`

`Peek()` only looks one character ahead. You need a second peek (position + 2)
to tell `->>` from `->`.

`ReadTwo` exists. Add a three-character reader (same shape as `ReadTwo`, length 3).
There is already `TwoCharTokenLength`; add a matching three-char constant
(code_check will flag a magic `3`).

In the `ch switch` in `Next()`, the `'-'` arm currently always emits minus.
Change it:
- if next char is `>` and the char after that is `>`, emit `JsonTextArrowToken` (length 3)
- else if next char is `>`, emit `JsonArrowToken` (length 2)
- else emit minus as today

A lone `-` must still be minus (`a - b`, unary `-1`).

### 3. `src/SqlParser/Parser.cs`

Near `ColonCastBindingPower`, add a binding power **higher than** `DotBindingPower`
and **lower than** `ColonCastBindingPower` (dot is 8, colon-cast is 9 — put arrows at 8
and bump colon-cast, or put arrows at 9 and colon-cast at 10). Goal: `a.b->'c'` is
`(a.b)->'c'`, and `x::json->>'k'` still casts first if you care; if colon-cast stays
tighter than arrows, `x::json->>'k'` is `(x::json)->>'k'`, which is what you want.

In `BindingPower`, map both new kinds to that arrow power. Do not add them to
`ComparisonBindingPower` (that is `>` / `>=`).

No new parse method. The existing binary loop will build `BinaryExpression`.

### 4. `src/SqlParser/Formatter.cs`

Named consts at the top of the class for the two operator strings (same as `Concat`
and `DoubleColon`). code_check rejects magic `"->"` / `"->>"`.

`AppendOperator` always inserts a space before the operator. `->` must not have
spaces. For the two new kinds: skip `AppendSpaceIfNeeded`, write the operator
string, then return. Other operators stay as they are.

`AppendSpaceIfNeeded` skips space after `(` `[` `.` `+` `-` `:`. After `->` the last
char is `>`. Add `>` to that skip set so the right-hand side is not written as
`-> 'a'`. Check that `x >= 1` still has spaces: after `>=` the last char is `=`,
not `>`, so it should still space before `1`.

`StartToken` already walks `BinaryExpression`. No change.

### 5. `src/SqlParser/StageDumper.cs`

No change.

### 6. `tests/SqlParser.Tests/FormatTests.cs`

Add facts (same `AssertFormatted` helper as the other tests):
- `metadata->'a'`
- `metadata->>'b'`
- chain `metadata->'a'->>'b'`
- qualified `l.metadata->'a'->>'b'`
- comment between the operator and the key

### 7. `sample.sql` and the sample golden

In the select list of `sample.sql`, add a JSON extract (e.g. on a metadata-style
column, or `p` with a string key). Keep it valid for this parser: identifier or
member access, then `->` / `->>`, then a string.

Update the expected output in `Formats_sample_query` in `FormatTests.cs` to match
(`->` / `->>` unchanged, string keys preserved, keywords/idents as usual).

### 8. Check

`code_check` on every file you touched, then `dotnet test tests/SqlParser.Tests`.
If `code_check` reports magic-literal, name the string/number as a const.
If `x >= 1` tests fail, the `>` skip set is too aggressive — fix spacing only for
the JSON operators, not for `>=`.
