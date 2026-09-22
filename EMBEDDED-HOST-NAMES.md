# Embedded host names

SQL-92: `<host parameter name>` is `<colon><identifier>` with no separator.
Embedded SQL `:foo`, Oracle bind `:id`. Same places as `?`.

New `EmbeddedHostExpression` (one `EmbeddedHost` token, text `:foo`). Not `?`
(already done). Not `$1`. Not `: foo` (space). Not `:"ident"`. Not indicator
parameters (`:a INDICATOR :b`).

Today `:` is unexpected unless the next char is `:` (`::` cast). `:foo` dies
on `:`.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: `:` then identifier start is one `EmbeddedHost` token. `::` still
   `DoubleColonToken`. Lone `:` still unexpected.
2. Parser: prefix → `EmbeddedHostExpression`.
3. Visitor: empty walk, like `HostParameterExpression`.
4. Tests, then `sample.sql`, then tick SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `:foo` lexes as
`[EmbeddedHost, EndOfFile]`.

### 1. `src/QlParse/SyntaxKind.cs`

Add `EmbeddedHost` next to `QuestionMark`. Do not add keywords. Do not add
`ColonToken`.

### 2. `src/QlParse/Lexer.cs`

The `':'` arm is currently: if next is `:`, `DoubleColonToken`, else throw.
Change the else: if `Peek()` is an identifier start, read colon plus the
identifier body (same `IsIdentifierPart` loop as `ReadIdentifierOrKeyword`)
as `EmbeddedHost`. Do not run `Keyword.Classify` — `:from` is EmbeddedHost,
not Colon plus `FromKeyword`.

`: foo` (space) and lone `:` still throw unexpected `:`. `::int` unchanged.

Name a const if `code_check` flags `':'`.

### 3. `src/QlParse/SyntaxNodes.cs`

Next to `HostParameterExpression`:

```
public sealed class EmbeddedHostExpression : Expression
{
    public required SyntaxToken Name { get; init; }
}
```

Span is the token span.

### 4. `src/QlParse/Parser.Prefix.cs`

`SyntaxKind.EmbeddedHost` → same shape as `HostParameter()`: Advance, return
`EmbeddedHostExpression`. Do not treat it as `IdentifierExpression`.

### 5. `src/QlParse/SqlVisitor.cs`

`case EmbeddedHostExpression` and empty `VisitEmbeddedHostExpression`.
Without this, `EmptyVisitor` throws on sample.sql.

### 6. `tests/QlParse.Tests/LexTests.cs`

- `:foo` and `:from` are one EmbeddedHost, text preserved
- `::` is still DoubleColonToken
- `:` and `: foo` still error (keep/add on `Unexpected_character_sets_error`)

### 7. `tests/QlParse.Tests/ParsePrefixTests.cs`

- `:foo` is `EmbeddedHostExpression`
- `x = :id` is `BinaryExpression` with EmbeddedHost on the right
- `a::int` is still `ColonCastExpression`

### 8. `sample.sql`

Add one `:name` in a comparison. `Parses_sample_sql` walks with `EmptyVisitor`.

### 9. `SQL-STANDARD.md`

Tick `Embedded host names`.

### 10. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `:from` is a keyword, you classified the host body. If `a::int` breaks,
the `':'` arm no longer prefers `::`.
