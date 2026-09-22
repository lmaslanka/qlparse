# Unicode delimited identifiers (`U&`)

SQL:2003 / Postgres: `U&"ident"` is a delimited identifier whose body may
contain Unicode escapes (`\0441`, `\+000061`). Same quote-doubling as `"ident"`.

Reuse `SyntaxKind.Identifier`. No new AST node. Not `U&'...'` (that is Unicode
string, a separate SQL-STANDARD.md item). Not `UESCAPE`. Do not decode `\XXXX`.

Today `U` is an identifier and `&` is unexpected, so `U&"foo"` fails in the
lexer. `"foo"` already works.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: `U`/`u` immediately followed by `&"` is one Identifier token, body
   same as quoted identifiers (`""` escape, unterminated / empty errors).
2. Parser: no change. Identifier already works as expression, table, alias,
   member.
3. Tests, then `sample.sql`, then tick SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `U&"foo"` lexes as
`[Identifier, EndOfFile]`.

### 1. `src/QlParse/Lexer.cs`

`Peek()` / `PeekTwo()` already exist. `&` is not a token today.

Name consts for the prefix (code_check rejects magic `'&'` / `2` if they are
not already named): ampersand character, prefix length 2 (`U` + `&`).

In `Next()`, before `IsIdentifierStart`, if `U`/`u` and next is `&` and the
char after that is `"`, read a unicode delimited identifier. `U` alone, `u`,
`Ufoo`, and `U & "foo"` (space) stay as today (`U` identifier; `&` still
unexpected).

`U&'foo'` must still fail: the third character is `'`, not `"`. Do not treat
that as an identifier and do not start a Unicode string.

`ReadQuotedIdentifier` assumes `_position` is on `"` and `start` is that
quote, so empty is `_position - start == TwoCharTokenLength`. For `U&""`
start is `U` and that check would miss. Generalize: empty means the quoted
body is empty (opening quote immediately closed, no `""` escape). Unterminated
message can stay "Unterminated quoted identifier". Empty message can stay
"Empty quoted identifier".

Keep the token text the full lexeme (`U&"foo"`), same as `"from"` keeps its
quotes. Do not interpret `\XXXX` / `\+XXXXXX`; backslash is just a character
inside the quotes.

A lone `"foo"` must still take the `IdentifierQuote` arm. `N'foo'` still takes
`IsPrefixedStringStart` first.

### 2. Parser / AST / keywords

No change. Do not add `UescapeKeyword`. Do not touch `Keyword.cs` or
`SyntaxKind.cs`.

### 3. `tests/QlParse.Tests/LexTests.cs`

Add facts/theories next to `Quoted_identifier`:

- `U&"foo"` and `u&"foo"` are one Identifier, text preserved
- `U&"a""b"` quote-doubling, text preserved
- `U&"\0441"` is one Identifier (escapes not decoded)
- `U&""` errors (empty)
- `U&"oops` errors (unterminated)
- `U` is still Identifier
- `U&'foo'` still errors (`&`)

### 4. `tests/QlParse.Tests/ParsePrefixTests.cs` and `ParseFromTests.cs`

- `U&"foo"` parses as `IdentifierExpression`
- `select * from U&"from"` parses as `TableReference` (same shape as
  `Parses_quoted_table_name`)

### 5. `sample.sql`

Add one unicode delimited identifier (column, alias, or table) that this
parser already accepts around it. `Parses_sample_sql` only checks parse
success.

### 6. `SQL-STANDARD.md`

Tick `Unicode delimited identifiers (\`U&\`)`. Leave `Unicode string (\`U&\`)`
unchecked.

### 7. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `code_check` reports magic-literal, name the character/length as a const.
If `U&'foo'` starts parsing, the third-character check is wrong — require `"`.
