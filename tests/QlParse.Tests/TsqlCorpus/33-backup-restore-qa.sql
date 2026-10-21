-- expect: Script(TsqlUseStatement | TsqlDeclareStatement,TsqlDeclareStatement,TsqlBackupStatement,TsqlRestoreStatement | TsqlBackupStatement | TsqlRestoreStatement | TsqlAlterDatabaseStatement | TsqlRestoreStatement | TsqlRestoreStatement | TsqlAlterDatabaseStatement | TsqlAlterDatabaseStatement | TsqlUseStatement | TsqlDbccStatement)
USE [master]
GO
DECLARE @stamp char(15) = FORMAT(SYSDATETIME(), 'yyyyMMdd_HHmmss');
DECLARE @file nvarchar(260) = N'\\backupsrv\sql\SalesDB\SalesDB_FULL_' + @stamp + N'.bak';

BACKUP DATABASE [SalesDB]
TO DISK = @file
WITH COMPRESSION, CHECKSUM, INIT, FORMAT, STATS = 10,
     NAME = N'SalesDB-Full Database Backup', DESCRIPTION = N'Nightly full backup';

RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;
GO
BACKUP LOG [SalesDB] TO DISK = N'\\backupsrv\sql\SalesDB\SalesDB_LOG.trn' WITH NOINIT, COMPRESSION, STATS = 25
GO
RESTORE FILELISTONLY FROM DISK = N'\\backupsrv\sql\SalesDB\SalesDB_FULL_20240314_010000.bak'
GO
ALTER DATABASE [SalesDB_QA] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
GO
RESTORE DATABASE [SalesDB_QA]
FROM DISK = N'\\backupsrv\sql\SalesDB\SalesDB_FULL_20240314_010000.bak'
WITH FILE = 1,
     MOVE N'SalesDB' TO N'E:\SQLData\SalesDB_QA.mdf',
     MOVE N'SalesDB_log' TO N'F:\SQLLogs\SalesDB_QA_log.ldf',
     NORECOVERY, REPLACE, STATS = 5
GO
RESTORE LOG [SalesDB_QA] FROM DISK = N'\\backupsrv\sql\SalesDB\SalesDB_LOG.trn' WITH RECOVERY, STOPAT = N'2024-03-14T06:00:00'
GO
ALTER DATABASE [SalesDB_QA] SET MULTI_USER
GO
ALTER DATABASE [SalesDB_QA] SET RECOVERY SIMPLE WITH NO_WAIT
GO
USE [SalesDB_QA]
GO
DBCC CHECKDB (N'SalesDB_QA') WITH NO_INFOMSGS, ALL_ERRORMSGS
GO
