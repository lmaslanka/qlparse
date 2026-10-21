namespace QlParse;

// SQL Server database, object and security words (workstream F). They stay plain identifiers in the lexer and
// are matched contextually under SqlOptions.SqlServer only, so none of them is reserved in ISO or Postgres mode.
internal static partial class Keyword
{
    internal const string SecActive = "active";
    internal const string SecApplication = "application";
    internal const string SecAssembly = "assembly";
    internal const string SecAudit = "audit";
    internal const string SecBlock = "block";
    internal const string SecCaller = "caller";
    internal const string SecCertificate = "certificate";
    internal const string SecClear = "clear";
    internal const string SecCollection = "collection";
    internal const string SecConfiguration = "configuration";
    internal const string SecContainment = "containment";
    internal const string SecCookie = "cookie";
    internal const string SecCounter = "counter";
    internal const string SecCredential = "credential";
    internal const string SecCryptographic = "cryptographic";
    internal const string SecDecryption = "decryption";
    internal const string SecDeny = "deny";
    internal const string SecEnable = "enable";
    internal const string SecEncryption = "encryption";
    internal const string SecExecutable = "executable";
    internal const string SecFile = "file";
    internal const string SecFilegroup = "filegroup";
    internal const string SecHashed = "hashed";
    internal const string SecLog = "log";
    internal const string SecLogin = "login";
    internal const string SecMaster = "master";
    internal const string SecMember = "member";
    internal const string SecModel = "model";
    internal const string SecModify = "modify";
    internal const string SecMustChange = "must_change";
    internal const string SecNoWait = "no_wait";
    internal const string SecOff = "off";
    internal const string SecOwner = "owner";
    internal const string SecPassword = "password";
    internal const string SecPredicate = "predicate";
    internal const string SecPrivate = "private";
    internal const string SecProvider = "provider";
    internal const string SecRegenerate = "regenerate";
    internal const string SecRemove = "remove";
    internal const string SecReplication = "replication";
    internal const string SecRevert = "revert";
    internal const string SecScoped = "scoped";
    internal const string SecSecondary = "secondary";
    internal const string SecSeconds = "seconds";
    internal const string SecServer = "server";
    internal const string SecService = "service";
    internal const string SecSignature = "signature";
    internal const string SecSpecification = "specification";
    internal const string SecTransfer = "transfer";
    internal const string SecWindows = "windows";
}
