# Fix plan

Fixed point: `origin/main` (`2e8d880`). Work is commit `93c7b1b`. Behavior stays. Performance is the constraint on every step.

Do these in order. 02 and 03 do not start until 01's tests are green. 02 and 03 do not touch each other.

| Plan | File | What it changes | Runtime cost |
| --- | --- | --- | --- |
| 01 | `plans/01-statement-dispatch.md` | Split statement dispatch from query parsing | SELECT gets faster. Required. |
| 02 | `plans/02-parser-file-seams.md` | Move parser methods into the file that owns them | None. File moves only. |
| 03 | `plans/03-nodes-and-visitor.md` | Split `SyntaxNodes.cs` and `SqlVisitor.cs` | None. No `Accept` unless a walk is measured hot. |

## Rules

- No predicate list, delegate table, dictionary, or virtual strategy on the parse path.
- Do not reserve more words in `Keyword.Classify`. That taxes every identifier.
- A statement lead that is already a `SyntaxKind` is one switch arm. No string compare.
- An identifier lead is one length bucket, then one `TextOf` compare, then a second-token switch. `SELECT` never enters that arm.
- Query position never calls statement dispatch.
- No new interface, wrapper, or base type. `Parser` stays one partial class. Nodes stay sealed classes in `namespace QlParse`.
- No allocation on the `SELECT` path: no `List`, no `string` materialization of the lead token, no `ToUpperInvariant` except the existing error path.
- Do not add BenchmarkDotNet. Time with a throwaway loop, then delete it.
- After each edit: `qlcheck` on the touched paths, then `dotnet test tests/QlParse.Tests`.
