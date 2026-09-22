# Character-set introducers

SQL-92: an identifier may be `_charset` immediately glued to the actual
identifier. Same introducer exists for strings (`_utf8'hello'`, MySQL).

Accept only the no-space forms:

- `_charset"ident"` → one `Identifier` (quoted body, `""` escape)
- `_charset'string'` → one `String` (same body as `'...'` / `N'...'`)

Reuse those two kinds. No new AST node. Do not validate charset names. Not
`N`/`B`/`X` prefixes (already done). Not space-separated `_charset ident`
(`select _x y` is already column `_x` alias `y`).

Today `_latin1"foo"` is two Identifier tokens and parses as name `_latin1`
with alias `"foo"`. `_latin1'foo'` is Identifier then String and fails.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests` and `ParsePrefixTests`. After
Write/Edit, run `code_check` on changed paths, then
`dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: after reading an identifier that starts with `_` and has at least
   one more character, if the next character is `"` or `'`, continue into
   that quoted body as the same token.
2. Parser: no change once it is one token.
3. Tests, then `sample.sql`, then tick SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `_latin1"foo"` lexes as
`[Identifier, EndOfFile]`.

### 1. `src/QlParse/Lexer.cs`

`ReadIdentifierOrKeyword` consumes `_latin1` and stops at `"`. After the
identifier body, if the lexeme starts with `_`, length > 1, and the current
character is `"` or `'`, finish as quoted identifier or string using the
original `start` (so the token text is `_latin1"foo"` / `_utf8'hi'`).

`_` alone must not introduce: `_"x"` stays Identifier `_` then quoted
`"x"` (or fail in parse). Charset name is the identifier parts after `_`.

Empty `_cs""` must error. `ReadQuotedIdentifier` currently treats empty as
token length 2, which is only right for unprefixed `""`. Empty means the
quoted body is empty (opening quote immediately closed). Same unterminated
messages as today. If unicode-delimited identifiers already generalized
this reader, reuse it; if not, fix the empty check here.

`_foo` with no following quote is still a normal identifier (`Keyword.Classify`
as today). `_foo bar` is still two tokens. Do not skip trivia between charset
and quote; space means "not an introducer".

`IsPrefixedStringStart` (`N'`, `B'`, `X'`) stays first. `_N'foo'` is an
introducer string, not national `N'foo'`.

Do not add `&` / `U&` handling here. Do not add a SyntaxKind.

### 2. Parser / AST / keywords

No change. Do not add charset fields to `IdentifierExpression` or
`LiteralExpression`.

### 3. `tests/QlParse.Tests/LexTests.cs`

Next to quoted identifiers / strings:

- `_latin1"foo"` one Identifier, text preserved
- `_UTF8"a""b"` quote-doubling
- `_utf8'hi'` one String, text preserved
- `_foo` still one Identifier
- `_latin1 "foo"` (space) is Identifier then Identifier, not one token
- `_cs""` errors (empty)
- `_cs"oops` errors (unterminated)

### 4. `tests/QlParse.Tests/ParsePrefixTests.cs` and `ParseFromTests.cs`

- `_latin1"foo"` is `IdentifierExpression`, not ident + alias
- `_utf8'hi'` is `LiteralExpression` with `SyntaxKind.String`
- `select * from _latin1"from"` is `TableReference` with a single name
  part, no alias (today it is table `_latin1` alias `"from"`)

Add a regression that `select _x y` is still ident `_x` with alias `y`.

### 5. `sample.sql`

Add one glued introducer identifier (and optionally one introducer string)
in a place that already parses. `Parses_sample_sql` only checks parse
success.

### 6. `SQL-STANDARD.md`

Tick `Character-set introducers`.

### 7. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `code_check` reports magic-literal, name the character as a const.
If `select _x y` loses its alias, the space-separated form leaked in — only
glue when `"` / `'` is the next source character, not after trivia.
