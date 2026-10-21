# QlParse

A hand-written, lossless SQL lexer and parser for .NET — covering the full ISO/IEC 9075 SQL standard (1992–2023), with opt-in PostgreSQL and SQL Server (T-SQL) dialect support, zero runtime dependencies, and a visitor API for walking the resulting syntax tree.

[![Build](https://github.com/lmaslanka/qlparse/actions/workflows/build.yml/badge.svg)](https://github.com/lmaslanka/qlparse/actions/workflows/build.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=lmaslanka_qlparse&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=lmaslanka_qlparse)
[![License: Apache 2.0](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](src/QlParse/QlParse.csproj)

## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Walking the syntax tree](#walking-the-syntax-tree)
- [Lexing only](#lexing-only)
- [Dialect flags](#dialect-flags)
- [SQL standard coverage](#sql-standard-coverage)
- [Project structure](#project-structure)
- [Building and testing](#building-and-testing)
- [Architecture](#architecture)
- [Roadmap](#roadmap)
- [License](#license)

## Features

- **Full ISO SQL grammar** — lexical rules, data types, scalar expressions, predicates, query expressions, DML, schema/DDL, constraints, privileges, transactions, cursors, dynamic SQL, triggers, SQL/PSM, typed tables, temporal tables, SQL/XML, JSON, row pattern matching, polymorphic table functions, SQL/MDA, and SQL/PGQ. See [SQL-STANDARD.md](SQL-STANDARD.md) for the full coverage checklist.
- **Lossless lexer** — every token carries its exact source span, and comments/whitespace are preserved as trivia rather than discarded, so the original text can always be reconstructed.
- **Hand-written recursive-descent parser** — no parser generator, no reflection-based grammar, just a straightforward `Parser` split by grammar area for readability.
- **Visitor API** — `SqlVisitor` walks the tree with one overridable `virtual` method per node type, so consumers only override what they care about.
- **Precise diagnostics** — a syntax error is a `SqlParseException` carrying the exact character offset where parsing failed.
- **Opt-in vendor dialects** — the PostgreSQL dialect (9.x–18) is gated behind `SqlOptions.Postgres` so standard SQL parsing is never affected unless you ask for it: dollar quoting, `$1` parameters, custom operators, array types, `ON CONFLICT`/`RETURNING`, partitioning, row-level security, utility commands, and PL/pgSQL function bodies parsed into the tree. The SQL Server dialect (2012–2025) is gated behind `SqlOptions.SqlServer` the same way: `[bracketed]` names, `@variables`, `GO`-separated batches with optional `;`, procedural T-SQL (`IF`/`WHILE`/`TRY...CATCH`, cursors, `EXEC`), `TOP`/`OUTPUT`/`MERGE`/`APPLY`/`PIVOT`, table hints, procedures, functions, triggers, and security, Service Broker and administration statements. See [POSTGRES-STANDARD.md](POSTGRES-STANDARD.md) and [SQLSERVER-STANDARD.md](SQLSERVER-STANDARD.md) for the full coverage lists.
- **Zero dependencies** — the `QlParse` library itself references nothing beyond the .NET base class library.
- **Architecture enforced by tests** — the folder/layering rules below aren't just convention; they're checked by an automated test suite. See [Architecture](#architecture).

## Installation

QlParse isn't published to NuGet.org yet — consume it directly from source:

```bash
git clone git@github.com:lmaslanka/qlparse.git
cd qlparse
dotnet build
```

Then either add a project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\qlparse\src\QlParse\QlParse.csproj" />
</ItemGroup>
```

or pack a local NuGet package:

```bash
dotnet pack src/QlParse -c Release -o ./nupkg
```

## Quick start

```csharp
using QlParse;

var result = Sql.Parse("select id, name from customers where id > 100");

if (result.Error is not null)
{
    Console.WriteLine($"Syntax error at offset {result.Error.Position}: {result.Error.Message}");
    return;
}

var select = (SelectStatement)result.Root!;
Console.WriteLine($"Selecting {select.SelectList.Count} column(s)");
```

`Sql.Parse` always returns a `SqlParseResult` — it never throws for invalid SQL. Check `result.Error` before touching `result.Root`.

## Walking the syntax tree

Override only the node types you care about; `SqlVisitor`'s base implementation recurses into everything else for you.

```csharp
using QlParse;

internal sealed class TableNameCollector(string source) : SqlVisitor
{
    public List<string> TableNames { get; } = [];

    public override void VisitTableReference(TableReference node)
    {
        TableNames.Add(string.Join('.', node.NameParts.Select(p => p.TextOf(source).ToString())));
        base.VisitTableReference(node);
    }
}

var result = Sql.Parse("select o.id from orders o join customers c on o.customer_id = c.id");
var collector = new TableNameCollector(result.Source);
collector.Visit(result.Root!);
// collector.TableNames => ["orders", "customers"]
```

## Lexing only

Need tokens without building a tree? `Sql.Lex` runs just the lexer:

```csharp
var lexed = Sql.Lex("select 1 -- comment");
foreach (var token in lexed.Tokens)
{
    Console.WriteLine($"{token.Kind}: {token.TextOf(lexed.Source)}");
}
```

## Dialect flags

`SqlOptions` is a `[Flags]` enum passed to `Sql.Lex`/`Sql.Parse`. With no flags, only standard SQL is accepted:

```csharp
// Standard SQL only (default)
Sql.Parse(sql);

// Enable the PostgreSQL dialect: $$dollar quoting$$, $1 parameters, ::casts, ON CONFLICT, PL/pgSQL bodies, ...
Sql.Parse(sql, SqlOptions.Postgres);

// Enable the SQL Server (T-SQL) dialect: [brackets], @variables, GO batches, optional ;, TRY/CATCH, MERGE, ...
Sql.Parse(sql, SqlOptions.SqlServer);
```

Under `SqlOptions.SqlServer` a script is parsed as a whole: a single statement comes back as itself, several statements as a `TsqlBatch`, and any `GO` line makes the root a `TsqlScript` of batches. Only the T-SQL reserved keywords are reserved, so columns named `date`, `time` or `position` work unquoted. Add `SqlOptions.QuotedIdentifierOff` to parse scripts written for `SET QUOTED_IDENTIFIER OFF`, where `"..."` is a string literal; it has no effect without `SqlOptions.SqlServer`.

```csharp
var result = Sql.Parse("""
    IF OBJECT_ID(N'dbo.Orders') IS NULL
        CREATE TABLE dbo.Orders ([Id] int IDENTITY(1,1) PRIMARY KEY, [date] date NOT NULL)
    GO
    SELECT TOP (10) [Id], date FROM dbo.Orders WITH (NOLOCK) ORDER BY date DESC
    """, SqlOptions.SqlServer);
// result.Root is a TsqlScript with two batches
```

## SQL standard coverage

| Document | What it tracks |
|---|---|
| [SQL-STANDARD.md](SQL-STANDARD.md) | ISO/IEC 9075 (1992–2023) grammar coverage — what qlparse already accepts |
| [POSTGRES-STANDARD.md](POSTGRES-STANDARD.md) | PostgreSQL-specific syntax beyond the ISO standard covered by `SqlOptions.Postgres` |
| [SQLSERVER-STANDARD.md](SQLSERVER-STANDARD.md) | SQL Server (T-SQL)-specific syntax beyond the ISO standard covered by `SqlOptions.SqlServer` |

## Project structure

```
src/QlParse/
├── Sql.cs, SqlOptions.cs, SqlLexResult.cs, SqlParseResult.cs, SqlParseException.cs   # public API
├── Lexing/       # tokenizer (internal)
├── Syntax/       # token/span primitives (public)
├── Parser/       # recursive-descent parser, split by grammar area (internal)
├── Visitor/      # SqlVisitor — walks the AST (public)
└── SyntaxNodes/  # one file per AST node type (public)
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for what belongs where and why.

## Building and testing

```bash
dotnet build          # builds QlParse, QlParse.Tests, and QlParse.ArchitectureTests
dotnet test           # runs the full test suite
```

The solution has three projects:

| Project | Purpose |
|---|---|
| `src/QlParse` | the library itself |
| `tests/QlParse.Tests` | lexer/parser/visitor behavior tests (xUnit) |
| `tests/QlParse.ArchitectureTests` | enforces the folder/layering rules in [ARCHITECTURE.md](ARCHITECTURE.md) |

## Architecture

QlParse is a single-assembly pipeline: `Lexing/` turns source text into tokens, `Parser/` turns tokens into an AST (`SyntaxNodes/`), and `Visitor/` walks that AST — `Sql.cs` is the composition root wiring it all together. These rules are locked in by `tests/QlParse.ArchitectureTests`, not just documentation. Full details, including every enforced rule, live in [ARCHITECTURE.md](ARCHITECTURE.md).

## Roadmap

- [x] Implement the PostgreSQL dialect extensions tracked in [POSTGRES-STANDARD.md](POSTGRES-STANDARD.md)
- [x] Implement the SQL Server (T-SQL) dialect extensions tracked in [SQLSERVER-STANDARD.md](SQLSERVER-STANDARD.md) behind `SqlOptions.SqlServer`
- [ ] Publish to NuGet.org

## License

Apache License 2.0 — see [LICENSE](LICENSE).
