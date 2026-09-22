# Host parameters (`?`)

SQL-92: `<dynamic parameter specification>` is a lone `?`. JDBC/ODBC style
positional parameter. Same places as a value expression: `x = ?`, `IN (?, ?)`,
`LIMIT ?`.

New `HostParameterExpression` (one `QuestionMark` token). Not `:name` (that is
Embedded host names). Not `$1`. Not `??`.

Today `?` is unexpected in the lexer.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: `?` is one `QuestionMark` token.
2. Parser: prefix expression → `HostParameterExpression`.
3. Visitor: walk the new node (empty, like `IdentifierExpression`).
4. Tests, then `sample.sql`, then tick SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `?` lexes as
`[QuestionMark, EndOfFile]`.

### 1. `src/QlParse/SyntaxKind.cs`

Add `QuestionMark` next to `Comma` / `Dot`. Do not add keywords.

### 2. `src/QlParse/Lexer.cs`

In the `ch switch` in `Next()`, add `'?' => ReadSingle(SyntaxKind.QuestionMark, …)`
next to the other single-char arms. Name a const if `code_check` flags `'?'`.

`??` is two `QuestionMark` tokens, not one. `:` stays unexpected unless `::`.

### 3. `src/QlParse/SyntaxNodes.cs`

Next to `IdentifierExpression`:

```
public sealed class HostParameterExpression : Expression
{
    public required SyntaxToken QuestionMark { get; init; }
}
```

Span is the token span (set in the parser like `Identifier()`).

### 4. `src/QlParse/Parser.Prefix.cs`

In `ParsePrefix`, add `SyntaxKind.QuestionMark` → a method the same shape as
`Identifier()`: Advance, return `HostParameterExpression` with that token.
Do not parse `?` as `LiteralExpression`.

### 5. `src/QlParse/SqlVisitor.cs`

Add a `case HostParameterExpression` in `Visit(Expression)` and
`VisitHostParameterExpression` with an empty body (same as identifier/literal).
Without this, `EmptyVisitor` throws on sample.sql.

### 6. `tests/QlParse.Tests/LexTests.cs`

Next to `Single_char_tokens`: `?` is `QuestionMark`.

### 7. `tests/QlParse.Tests/ParsePrefixTests.cs`

- `?` is `HostParameterExpression`
- `select x = ?` parses (`BinaryExpression` with `?` on the right)

### 8. `sample.sql`

Add one `?` in a comparison or `IN` list. `Parses_sample_sql` walks the tree
with `EmptyVisitor`.

### 9. `SQL-STANDARD.md`

Tick `Host parameters (\`?\`)`. Leave `Embedded host names` unchecked.

### 10. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `select ??` is one parameter, longest-match ate both — emit one `?` at a
time. If the visitor throws `Unknown query`/`Unknown expression`, the new
case is missing.
