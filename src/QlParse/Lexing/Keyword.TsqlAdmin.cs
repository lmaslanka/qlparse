using System.Collections.Frozen;

namespace QlParse;

// SQL Server administration, Service Broker and full-text words (workstream G). They stay plain identifiers
// in the lexer and are matched by text only under SqlOptions.SqlServer, so none of them is reserved elsewhere.
internal static partial class Keyword
{
    internal const string AdminAvailability = "availability";
    internal const string AdminBackup = "backup";
    internal const string AdminBuffer = "buffer";
    internal const string AdminBulk = "bulk";
    internal const string AdminCheckpoint = "checkpoint";
    internal const string AdminCleanup = "cleanup";
    internal const string AdminCollection = "collection";
    internal const string AdminConfiguration = "configuration";
    internal const string AdminContract = "contract";
    internal const string AdminConversation = "conversation";
    internal const string AdminDatabase = "database";
    internal const string AdminDbcc = "dbcc";
    internal const string AdminDeny = "deny";
    internal const string AdminDescription = "description";
    internal const string AdminDialog = "dialog";
    internal const string AdminEndpoint = "endpoint";
    internal const string AdminEvent = "event";
    internal const string AdminFailover = "failover";
    internal const string AdminFileGroup = "filegroup";
    internal const string AdminFileListOnly = "filelistonly";
    internal const string AdminFulltext = "fulltext";
    internal const string AdminGovernor = "governor";
    internal const string AdminHadr = "hadr";
    internal const string AdminHeaderOnly = "headeronly";
    internal const string AdminHoldLock = "holdlock";
    internal const string AdminJob = "job";
    internal const string AdminKill = "kill";
    internal const string AdminLabelOnly = "labelonly";
    internal const string AdminList = "list";
    internal const string AdminListener = "listener";
    internal const string AdminLog = "log";
    internal const string AdminMaster = "master";
    internal const string AdminMessage = "message";
    internal const string AdminMirror = "mirror";
    internal const string AdminMove = "move";
    internal const string AdminNoReset = "noreset";
    internal const string AdminNotification = "notification";
    internal const string AdminNowait = "nowait";
    internal const string AdminOff = "off";
    internal const string AdminOverride = "override";
    internal const string AdminPool = "pool";
    internal const string AdminPopulation = "population";
    internal const string AdminProcess = "process";
    internal const string AdminQuery = "query";
    internal const string AdminQueue = "queue";
    internal const string AdminReadText = "readtext";
    internal const string AdminReceive = "receive";
    internal const string AdminReconfigure = "reconfigure";
    internal const string AdminReplica = "replica";
    internal const string AdminReset = "reset";
    internal const string AdminResource = "resource";
    internal const string AdminRestore = "restore";
    internal const string AdminRewindOnly = "rewindonly";
    internal const string AdminRoute = "route";
    internal const string AdminSend = "send";
    internal const string AdminSent = "sent";
    internal const string AdminServer = "server";
    internal const string AdminService = "service";
    internal const string AdminSession = "session";
    internal const string AdminSetUser = "setuser";
    internal const string AdminShutdown = "shutdown";
    internal const string AdminStatisticalSemantics = "statistical_semantics";
    internal const string AdminStatistics = "statistics";
    internal const string AdminStats = "stats";
    internal const string AdminStatusOnly = "statusonly";
    internal const string AdminStoplist = "stoplist";
    internal const string AdminTimer = "timer";
    internal const string AdminTimeout = "timeout";
    internal const string AdminTop = "top";
    internal const string AdminUpdateText = "updatetext";
    internal const string AdminValidation = "validation";
    internal const string AdminValidXml = "valid_xml";
    internal const string AdminVerifyOnly = "verifyonly";
    internal const string AdminWorkload = "workload";
    internal const string AdminWriteText = "writetext";

    // Size and duration units that may follow a number in an option value (`MAX_MEMORY = 4096 KB`).
    internal static bool IsTsqlAdminUnit(ReadOnlySpan<char> text) => TsqlAdminSets.Units.Contains(text);

    // Kept in their own type so the set is built on first T-SQL use, never when ISO code touches Keyword.
    internal static class TsqlAdminSets
    {
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> Units = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "kb", "mb", "gb", "tb", "percent", "ms", "milliseconds", "second", "seconds", "minute", "minutes",
            "hour", "hours", "day", "days")
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
