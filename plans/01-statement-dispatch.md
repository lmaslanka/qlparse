# 01 — Statement dispatch

`ParseQuery` is the query-expression parser. `ParseStatement` is the only place that recognizes statements. Today both are the same 100-predicate ladder in `Parser.Query.cs`, and every subquery pays it.

## Done when

- A `SELECT`, a parenthesized query, a `UNION` right side, a CTE body, and an `IN (SELECT …)` never call `ParseStatement`.
- A script statement, a PSM body, a routine body, a handler action, and a module statement enter through one switch, not a ladder.
- `dotnet test tests/QlParse.Tests` is unchanged and green.
- A throwaway `SELECT` loop is not slower than before this change. Delete the harness.

## Shape

```text
ParseStatement()
  peek past "label :" without consuming
  switch lead.Kind
    Select | With | Values | OpenParen -> ParseQuery()
    Update                              -> ParseUpdate()
    Set                                 -> ParseSetStatement()
    Fetch                               -> ParseFetchStatement()
    Case                                -> ParseCaseStatement()
    For                                 -> ParseForStatement()
    End                                 -> declare-section or ParseQuery()
    Identifier                          -> ParseIdentifierStatement()
    default                             -> ParseQuery()

ParseQuery(minBindingPower = 0)
  if minBindingPower == 0 && With -> ParseWithQuery()
  return ParseSetOp(minBindingPower)
```

`ParseQuery` keeps `minBindingPower`. Do not "fix" `ParseQuery(1)` on the `WITH` body. That call is what rejects a nested `WITH` at binding power > 0. `(WITH …)` still works because `ParseParenQuery` calls `ParseQuery()` at power 0.

`ParseParenQuery` keeps calling `ParseQuery`, not `ParseStatement`. No test covers `(INSERT …)`. That acceptance is an accident of the ladder. Do not add a flag to preserve it. If a test fails on it, the test is the spec: handle that one case inside the paren parser. Do not put statement dispatch back on the query path.

## Call sites that become `ParseStatement`

- `Parser.cs` script loop (both `ParseQuery` calls)
- `Parser.Module.cs` module contents, procedure body, embedded SQL body
- `Parser.Psm.cs` handler action, compound statement list
- `Parser.Routine.cs` trigger body, `SQL` routine body

## Call sites that stay `ParseQuery`

- `Parser.Expression.cs` `IN (query)`
- `Parser.From.cs` derived tables
- `Parser.Prefix.cs` scalar subqueries
- `Parser.Collection.cs` (already gated by `IsQueryStart`)
- `Parser.Dml.cs` insert source
- `Parser.Ddl.cs` / `Parser.View.cs` `AS` query
- `Parser.Cursor.cs` cursor query
- `Parser.Psm.cs` `FOR` query (the `ParseQuery` above `DO`, not the statement list)
- `Parser.Mda.cs`, `Parser.Ptf.cs`, `Parser.Markup.cs`
- `Parser.Query.cs` `WITH` body, CTE body, set-op right, paren inner

## Identifier arm

Entered only when the lead is `SyntaxKind.Identifier`. Switch on span length. Inside a bucket, one `TokenEquals` per verb of that length, then return. Do not compare the same word once per statement kind.

Families, each a second-token switch. Copy today's lookahead into the family. Delete it from `ParseQuery`.

- `CREATE`: second token, plus the lookaheads that are not a single next word. `GLOBAL` / `LOCAL` then table. `RECURSIVE` then view. `UNIQUE` / `CLUSTERED` / `NONCLUSTERED` then index. `INSTANCE` / `STATIC` / `CONSTRUCTOR` then method. `CHARACTER` then `SET`. `CAST` is `CastKeyword`, not an identifier. `PROPERTY` is the graph arm.
- `ALTER` and `DROP`: second token. Routine designator lookahead stays inside these arms (`NextIsRoutineDesignator`).
- `DECLARE`: cursor lookahead, else handler (`CONTINUE` / `EXIT` / `UNDO`), else variable. Not "declare and not the other two."
- `ALLOCATE`: cursor lookahead, else descriptor. Both checks run only in this arm.
- `EXECUTE`: `IMMEDIATE`, else bare execute.
- `BEGIN`: `DECLARE SECTION`, else compound. `END` is the declare-section arm above, not this bucket.
- Single-verb identifiers (`INSERT`, `DELETE`, `MERGE`, `GRANT`, `CALL`, …) call the existing `Parse*` method. Keep the second-token check that the `Parse*` method already requires (`INSERT INTO`, `DELETE FROM`). Do not re-check it in the switch and again in the parser.

`SET` is already `SetKeyword`. `ParseSetStatement` switches on the next token (`TRANSACTION`, `LOCAL TRANSACTION`, `CONSTRAINTS`, `SESSION`, `ROLE`, `TIME ZONE`, `CATALOG`, `SCHEMA`, `PATH`, `NAMES`, `CHARACTER SET`, `COLLATION`, `CONNECTION`). The default arm is `ParseSetAssignment`. That default lives here, not at the bottom of a global ladder.

Labeled `BEGIN` / `LOOP` / `WHILE` / `REPEAT` / `FOR`: if the current token is an identifier and the next is `:`, dispatch on the token after the colon. Do not consume. `ParseBeginningLabel` still consumes.

## Delete

After the switch calls `Parse*` directly, delete `Is*` methods that exist only to feed the ladder (`IsInsert`, `IsCreateTable`, `IsSetAssignment`, …). Keep `Is*` used inside a parser (`IsQueryStart`, `IsGraphTable`, `IsTransactionMode`, `IsLabeled`, …).

Do not replace the ladder with a list of `(predicate, parser)` pairs.

## Measure

Before editing, time a loop that parses a fixed multi-statement `SELECT` script and a fixed `CREATE TABLE` script. `Stopwatch`, no new package, harness not committed. After the switch, `SELECT` median must not rise. `CREATE` should drop. Then `qlcheck` the touched parser files and `dotnet test tests/QlParse.Tests`.
