# Unicode string (`U&`)

SQL:2003 / Postgres: `U&'string'` is a character string whose body may contain
Unicode escapes (`\0441`, `\+000061`). Same quote-doubling as `'string'`.

Reuse `SyntaxKind.String`. No new AST node. Not `U&"..."` (already done:
Unicode delimited identifiers). Not `UESCAPE`. Do not decode `\XXXX`.

Today `IsUnicodeDelimitedIdentifierStart` requires `"`, so `U&'foo'` is
identifier `U` then unexpected `&`. `LexTests.Unexpected_character_sets_error`
asserts that. `'foo'` and `N'foo'` already work.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: `U`/`u` immediately followed by `&'` is one String token, body same
   as ordinary strings (`''` escape, unterminated error). Empty `U&''` is
   valid (unlike `U&""`).
2. Parser: no change. `LiteralExpression` already accepts String.
3. Tests (including removing `U&'foo'` from unexpected-character), then
   `sample.sql`, then tick SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `U&'foo'` lexes as
`[String, EndOfFile]`.

### 1. `src/QlParse/Lexer.cs`

`Ampersand`, `UnicodeDelimitedPrefixLength`, `PeekTwo()`, and
`ReadString(..., start)` already exist. `ReadUnicodeDelimitedIdentifier`
skips `U&` then reads a quoted identifier.

In `Next()`, next to the unicode-identifier check: if `U`/`u` and next is
`&` and the char after that is `'`, skip the prefix and
`ReadString(triviaStart, triviaCount, start)` with start at `U`. Token text
is the full lexeme (`U&'foo'`).

`U&"foo"` must still be Identifier. `U` alone, `u& 'foo'` (space), and
`U & 'foo'` stay as today.

Do not interpret `\XXXX` / `\+XXXXXX`. Do not add `UescapeKeyword`. Do not
touch `Keyword.cs` or `SyntaxKind.cs`.

Empty `U&''` is a String, not an error. Unterminated `U&'oops` uses the
existing "Unterminated string" message.

### 2. Parser / AST

No change.

### 3. `tests/QlParse.Tests/LexTests.cs`

Next to `Strings` / `Unicode_delimited_identifier`:

- `U&'foo'` and `u&'foo'` are one String, text preserved
- `U&'it''s'` quote-doubling, text preserved
- `U&'\0441'` is one String (escapes not decoded)
- `U&''` is one String (empty is allowed)

Remove `U&'foo'` from `Unexpected_character_sets_error`. Add `U&'oops` to
`Unterminated_lexemes_set_error`.

### 4. `tests/QlParse.Tests/ParsePrefixTests.cs`

Add `U&'foo'` to `Parses_literals` as `SyntaxKind.String`.

### 5. `sample.sql`

Add one unicode string in a place that already parses (select list or a
comparison). `Parses_sample_sql` only checks parse success.

### 6. `SQL-STANDARD.md`

Tick `Unicode string (\`U&\`)`.

### 7. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `U&"foo"` becomes a String, the third-character check used `'` for both
forms — identifiers stay `"`, strings stay `'`.
