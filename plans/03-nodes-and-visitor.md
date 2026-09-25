# 03 — Nodes and visitor

Independent of plan 02. Do not start until plan 01 is green. Do not edit `Parser.*`.

This is a file split. Public types, properties, and `SqlVisitor.Visit(...)` signatures stay. No new base class. No interface. No source generator.

## Done when

`SyntaxNodes.cs` and `SqlVisitor.cs` are each under 900 lines, by adding files in the same namespace. `dotnet test tests/QlParse.Tests` is green. `qlcheck` on the new files. Parse timing from plan 01 is unchanged, because this plan does not run during parse.

## Syntax nodes

Move sealed classes out of `SyntaxNodes.cs` into files named for the parser that builds them: `SyntaxNodes.Query.cs`, `SyntaxNodes.Expression.cs`, `SyntaxNodes.From.cs`, `SyntaxNodes.Ddl.cs`, `SyntaxNodes.View.cs`, `SyntaxNodes.Dml.cs`, `SyntaxNodes.Psm.cs`, and so on for the rest. `Query` and `Expression` stay in `SyntaxNodes.cs` if other files need the bases; they are small.

A class moves with its nested payload types (`SearchClause` with `WithQuery`, not into a junk file). Recount. Split again on the next class boundary if a file is at or over 900.

Do not collapse optional keyword tokens. That is the concrete tree. Changing it changes the API and the allocation shape.

## Visitor

`SqlVisitor` becomes `public abstract partial class SqlVisitor`. Split on the existing `Visit` overloads, and take that overload's `VisitX` methods with it:

- `SqlVisitor.Query.cs` — `Visit(Query)` and the query cases
- `SqlVisitor.Expression.cs` — `Visit(Expression)`
- `SqlVisitor.Table.cs` — `Visit(TableSource)`, `Visit(JoinConstraint)`
- one file per remaining overload (`TableElement`, `AlterViewAction`, `AlterDomainAction`, `AlterTableAction`, `MergeAction`, `RowPattern`)

The type switch stays. Splitting the file does not make the walk faster, and it must not make it slower. Do not reorder cases. Do not replace the switch with a dictionary.

## Accept is not this plan

A 110-arm type test is slower than one virtual call if a consumer walks the tree. It is not on the parse path. Do not add `Accept` in this change.

Add it only as a later, separate change, and only if a throwaway loop shows a full walk of a large tree costing more than parsing that tree. Shape, if that measurement says so: one abstract `Accept(SqlVisitor)` on the root being walked, each sealed override calls the matching `VisitX`, and `Visit(Query)` becomes `query.Accept(this)`. Parse code never calls it. No second visitor interface.
