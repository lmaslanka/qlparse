# Architecture

QlParse is a single-assembly pipeline: `Lexing/` turns source text into tokens, `Parser/` turns tokens into an AST (`SyntaxNodes/`), and `Visitor/` walks that AST. `Sql.cs`, at the root, is the composition root — the only place the lexing/parsing pipeline is wired together and exposed publicly.

| Folder | Holds | Visibility |
|---|---|---|
| (root) | `Sql`, `SqlOptions`, `SqlLexResult`, `SqlParseResult`, `SqlParseException` — the public entry surface | public |
| `Lexing/` | `Lexer`, `Keyword`, `Keyword.Classify` — tokenizing | internal |
| `Syntax/` | `SourceSpan`, `SyntaxToken`, `SyntaxTrivia`, `SyntaxKind` — token/span primitives | public |
| `Parser/` | `Parser` (partial, split by grammar area) — builds the AST | internal |
| `Visitor/` | `SqlVisitor` (partial, `virtual` methods, designed for subclassing) — walks the AST | public |
| `SyntaxNodes/` | one file per AST node type — pure data, no behavior | public |

These rules are enforced by `tests/QlParse.ArchitectureTests`. Every rule failure names the rule id below; there is no baseline/grandfathering file — any violation must be fixed, not suppressed.

## A1 — Parser/ and Lexing/ types are internal implementation details

Only `Sql.cs` may expose the lexing/parsing pipeline. Nothing under `Parser/` or `Lexing/` may be `public`.

## A2 — Everything outside Parser/ and Lexing/ is public API surface

`Syntax/`, `Visitor/`, `SyntaxNodes/`, and the root are the library's public contract; types there must be `public`.

## A3 — SyntaxNodes/ must not reference Parser/Lexing types

AST node types are pure data holders. They must not depend on `Parser`, `Lexer`, `Keyword`, or anything else that lives under `Parser/`/`Lexing/` — a node shouldn't need to know how it was produced.

## A4 — Visitor/ must not reference Parser/Lexing types

`SqlVisitor` only walks an already-built AST. It must not depend on `Parser`, `Lexer`, `Keyword`, or anything else under `Parser/`/`Lexing/`.

## A5 — Parser/ must not reference Visitor types

Dependency direction is one-way: lex → parse → visit. `Parser` must not know `SqlVisitor` exists.

## A6 — File name matches a type declared in it

A file's name, up to its first `.`, must match the name of a type declared in it (e.g. `Parser.Ddl.cs` declares `partial class Parser`; `SelectStatement.cs` declares `SelectStatement`). Keeps navigation predictable across ~340 files.

## A7 — Concrete SyntaxNodes/ types are sealed

AST node types are not designed for further inheritance; every concrete (non-abstract) type under `SyntaxNodes/` must be `sealed`.
