namespace QlParse;

// Postgres routine attributes and PL/pgSQL words. They stay plain identifiers in the lexer and are
// matched contextually, so none of them is reserved in ISO mode.
internal static partial class Keyword
{
    internal const string RoutineImmutable = "immutable";
    internal const string RoutineStable = "stable";
    internal const string RoutineVolatile = "volatile";
    internal const string RoutineStrict = "strict";
    internal const string RoutineLeakproof = "leakproof";
    internal const string RoutineParallel = "parallel";
    internal const string RoutineSafe = "safe";
    internal const string RoutineRestricted = "restricted";
    internal const string RoutineUnsafe = "unsafe";
    internal const string RoutineCost = "cost";
    internal const string RoutineDefiner = "definer";
    internal const string RoutineInvoker = "invoker";
    internal const string RoutineSupport = "support";
    internal const string RoutineSetof = "setof";
    internal const string RoutinePlpgsql = "plpgsql";
    internal const string PlpgsqlAlias = "alias";
    internal const string PlpgsqlConstant = "constant";
    internal const string PlpgsqlElsif = "elsif";
    internal const string PlpgsqlReverse = "reverse";
    internal const string PlpgsqlForeach = "foreach";
    internal const string PlpgsqlSlice = "slice";
    internal const string PlpgsqlRaise = "raise";
    internal const string PlpgsqlPerform = "perform";
    internal const string PlpgsqlAssert = "assert";
    internal const string PlpgsqlQuery = "query";
    internal const string PlpgsqlStacked = "stacked";
    internal const string PlpgsqlMove = "move";
    internal const string PlpgsqlForward = "forward";
    internal const string PlpgsqlBackward = "backward";
    internal const string PlpgsqlDebug = "debug";
    internal const string PlpgsqlLog = "log";
    internal const string PlpgsqlInfo = "info";
    internal const string PlpgsqlNotice = "notice";
    internal const string PlpgsqlWarning = "warning";
    internal const string PlpgsqlRowtype = "rowtype";
}
