# Simple comment alternatives in standard

SQL-92: `<simple comment introducer>` is two **or more** consecutive minus
signs with no separator (`--`, `---`, `----`). Extra minuses are still a
simple comment, not minus then comment. Later SQL keeps the same `--` line
comment; extra minuses fall into the comment body.

Reuse `SyntaxKind.LineCommentTrivia`. No new trivia kind. Not `#` / `//` /
`{ }` (vendor). Not Unicode NEL / LS / PS as extra newlines.

Today `ReadLineComment` starts at `--` and runs to `\n` / `\r` / EOF, so
`---` is already trivia. This item is untested coverage, not a new lexeme.

Seam: `Sql.Lex` / `Sql.Parse` via `LexTests`. After Write/Edit, run
`code_check` on changed paths, then `dotnet test tests/QlParse.Tests`.

---

## Plan

1. Lexer: no change unless a test fails. Extra minuses after `--` stay in
   the same line comment.
2. Tests that `---`, glued `--`, and `--` at EOF are trivia, then tick
   SQL-STANDARD.md.

---

## Instructions

Work one failing test at a time. First test: `--- c\nselect` lexes as
`[SelectKeyword, EndOfFile]` with `LineCommentTrivia` on `select`.

### 1. `src/QlParse/Lexer.cs`

Do not add a second comment path. Do not treat `---` as minus plus `--`.
Do not terminate comments on characters other than `\n` and `\r`.

If `---` is somehow minus then comment, the trivia start is wrong: the
introducer is the whole run of minuses, but as trivia the full `--…`
lexeme already includes them. Leave `ReadLineComment` as it is.

### 2. Parser / AST / SyntaxKind

No change.

### 3. `tests/QlParse.Tests/LexTests.cs`

Next to `Line_comment_is_leading_trivia`:

- `--- c\nselect` is SelectKeyword with LineCommentTrivia
- `select 1---x` is SelectKeyword, Number, EndOfFile (comment on EOF)
- `select--c` (no space) is SelectKeyword then EOF with LineCommentTrivia
- `# c\nselect` still errors (`#` unexpected), not a comment

### 4. `sample.sql`

Optional. `Parses_sample_sql` already has `--` at the top. A `---` in the
select list as trivia is enough if you add one.

### 5. `SQL-STANDARD.md`

Tick `Simple comment alternatives in standard`. Leave host parameters and
embedded host names unchecked.

### 6. Check

`code_check` on every file you touched, then `dotnet test tests/QlParse.Tests`.
If `# c` starts parsing, you added a vendor comment — do not.
