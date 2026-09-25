# 02 — Parser file seams

Start only after plan 01 is green. This plan moves methods. It does not change signatures, control flow, or allocations.

`Parser` stays `internal sealed partial class Parser`. No helper type. No interface.

## Done when

Every parser file is under 900 lines. `ParseRoutineParameter` and `ParseCharacteristic` live in `Parser.Routine.cs`. `Parser.View.cs` parses views only. `dotnet test tests/QlParse.Tests` is green. `qlcheck` on every moved file.

## Moves

`ExpectIdent` and `NextEquals` live in `Parser.Ddl.cs` and are used by every partial. Move them to `Parser.cs` next to `Expect`. That is a move, not a new home for "utilities."

`Parser.View.cs` (1432) is four owners:

| Lines (approx) | Destination | Contents |
| --- | --- | --- |
| 1–172 | `Parser.View.cs` | create / alter / drop view |
| 173–243 | `Parser.Domain.cs` | domain |
| 244–1126 | `Parser.Type.cs` | type, ordering, cast, transform |
| 1127–end | `Parser.Routine.cs` | `ParseRoutineParameter`, `ParseCharacteristic` |

`Parser.Routine.cs` is 494. Adding the two helpers lands near 800. If `Parser.Type.cs` is still at or over 900, split transform (`ParseCreateTransform` through `ParseTransformElement`) into `Parser.Transform.cs`. Do not split earlier than that.

`Parser.Ddl.cs` (1543):

| Lines (approx) | Destination | Contents |
| --- | --- | --- |
| schema statements, `ParseSchemaNameList`, `ParseDropBehavior` | `Parser.Schema.cs` | schema |
| `ParseCreateTable` through `ParseRefIs` | `Parser.Table.cs` | table, column, like, on commit |
| `ParseConstraint` through generation / sequence options | `Parser.Constraint.cs` | constraints, references, identity |

Recount after the move. If a file is still at or over 900, split on the next method boundary. Do not extract a "shared DDL context" object to get under the line.

`Parser.Prefix.cs` (1385, was already over 1k):

- Leave `ParsePrefix`'s `SyntaxKind` switch in `Parser.Prefix.cs`. That switch is the expression hot path. Do not wrap it, and do not add a second lookup in front of it.
- Move `ParseIdentifierPrefix` and the methods only it calls into `Parser.Prefix.Name.cs`.
- Stop when `Parser.Prefix.cs` is under 900. If the name file is over 900, split on the next method boundary inside that file only.

## Do not

- Rename `Query` to `Statement`. That is an API change, not a seam fix.
- Move statement dispatch out of `Parser.Query.cs` into a new type.
- Touch `Keyword.Classify` or the lexer.
