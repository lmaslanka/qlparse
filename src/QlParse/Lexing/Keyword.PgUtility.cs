namespace QlParse;

// Postgres utility, session and security words. They stay plain identifiers in the lexer and are
// matched contextually, so none of them is reserved in ISO mode.
internal static partial class Keyword
{
    internal const string UtilAbort = "abort";
    internal const string UtilAccess = "access";
    internal const string UtilAnalyse = "analyse";
    internal const string UtilAnalyze = "analyze";
    internal const string UtilBypassRls = "bypassrls";
    internal const string UtilCheckpoint = "checkpoint";
    internal const string UtilCluster = "cluster";
    internal const string UtilCreateDb = "createdb";
    internal const string UtilCreateRole = "createrole";
    internal const string UtilDatabase = "database";
    internal const string UtilDiscard = "discard";
    internal const string UtilEnable = "enable";
    internal const string UtilEncrypted = "encrypted";
    internal const string UtilEvent = "event";
    internal const string UtilExclusive = "exclusive";
    internal const string UtilExplain = "explain";
    internal const string UtilExtension = "extension";
    internal const string UtilForce = "force";
    internal const string UtilFreeze = "freeze";
    internal const string UtilFunctions = "functions";
    internal const string UtilInherit = "inherit";
    internal const string UtilListen = "listen";
    internal const string UtilLoad = "load";
    internal const string UtilLock = "lock";
    internal const string UtilLogin = "login";
    internal const string UtilMaintain = "maintain";
    internal const string UtilMapping = "mapping";
    internal const string UtilMaterialized = "materialized";
    internal const string UtilMode = "mode";
    internal const string UtilNotify = "notify";
    internal const string UtilNowait = "nowait";
    internal const string UtilOwned = "owned";
    internal const string UtilOwner = "owner";
    internal const string UtilPassword = "password";
    internal const string UtilPermissive = "permissive";
    internal const string UtilPlans = "plans";
    internal const string UtilPolicy = "policy";
    internal const string UtilPrepared = "prepared";
    internal const string UtilProcedural = "procedural";
    internal const string UtilProcedures = "procedures";
    internal const string UtilReassign = "reassign";
    internal const string UtilReplication = "replication";
    internal const string UtilReset = "reset";
    internal const string UtilRestrictive = "restrictive";
    internal const string UtilRoutines = "routines";
    internal const string UtilSchemas = "schemas";
    internal const string UtilSecurity = "security";
    internal const string UtilSequences = "sequences";
    internal const string UtilServer = "server";
    internal const string UtilSuperuser = "superuser";
    internal const string UtilSysid = "sysid";
    internal const string UtilTablespace = "tablespace";
    internal const string UtilTemp = "temp";
    internal const string UtilTypes = "types";
    internal const string UtilUnencrypted = "unencrypted";
    internal const string UtilUnlisten = "unlisten";
    internal const string UtilVacuum = "vacuum";
    internal const string UtilValid = "valid";
    internal const string UtilVerbose = "verbose";
}
