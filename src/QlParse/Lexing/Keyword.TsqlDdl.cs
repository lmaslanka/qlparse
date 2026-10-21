namespace QlParse;

// SQL Server DDL words (workstream D: tables, partitioning, indexes, views, synonyms, types, graph and
// constraints). All are matched by text in T-SQL-only parser paths and stay non-reserved identifiers.
internal static partial class Keyword
{
    internal const string TsqlDdlAbort = "abort";
    internal const string TsqlDdlAllSparseColumns = "all_sparse_columns";
    internal const string TsqlDdlColumnSet = "column_set";
    internal const string TsqlDdlColumnstore = "columnstore";
    internal const string TsqlDdlDatabase = "database";
    internal const string TsqlDdlEncrypted = "encrypted";
    internal const string TsqlDdlFilestream = "filestream";
    internal const string TsqlDdlFilestreamOn = "filestream_on";
    internal const string TsqlDdlFiletable = "filetable";
    internal const string TsqlDdlHash = "hash";
    internal const string TsqlDdlHidden = "hidden";
    internal const string TsqlDdlLastNode = "last_node";
    internal const string TsqlDdlMasked = "masked";
    internal const string TsqlDdlNocheck = "nocheck";
    internal const string TsqlDdlNode = "node";
    internal const string TsqlDdlPartitions = "partitions";
    internal const string TsqlDdlPause = "pause";
    internal const string TsqlDdlPersisted = "persisted";
    internal const string TsqlDdlProc = "proc";
    internal const string TsqlDdlReplication = "replication";
    internal const string TsqlDdlResume = "resume";
    internal const string TsqlDdlRowguidcol = "rowguidcol";
    internal const string TsqlDdlScheme = "scheme";
    internal const string TsqlDdlSequenceNumber = "sequence_number";
    internal const string TsqlDdlServer = "server";
    internal const string TsqlDdlShortestPath = "shortest_path";
    internal const string TsqlDdlSparse = "sparse";
    internal const string TsqlDdlSpatial = "spatial";
    internal const string TsqlDdlSplit = "split";
    internal const string TsqlDdlSwitch = "switch";
    internal const string TsqlDdlSynonym = "synonym";
    internal const string TsqlDdlTextImageOn = "textimage_on";
    internal const string TsqlDdlTransactionId = "transaction_id";
    internal const string TsqlDdlUsed = "used";
    internal const string TsqlDdlVector = "vector";
}
