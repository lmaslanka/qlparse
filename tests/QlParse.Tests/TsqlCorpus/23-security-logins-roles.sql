-- expect: Script(TsqlUseStatement | TsqlIfStatement | TsqlCreatePrincipalStatement | TsqlUseStatement | TsqlCreatePrincipalStatement,TsqlCreatePrincipalStatement,TsqlCreatePrincipalStatement | TsqlAlterPrincipalStatement,TsqlAlterPrincipalStatement,TsqlPermissionStatement,TsqlPermissionStatement,TsqlPermissionStatement,TsqlPermissionStatement,TsqlPermissionStatement,TsqlPermissionStatement | TsqlExecuteAsStatement,SelectStatement,SelectStatement,TsqlRevertStatement)
USE [master]
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'svc_reporting')
    CREATE LOGIN [svc_reporting] WITH PASSWORD = N'Str0ng!Passw0rd#2024', DEFAULT_DATABASE = [SalesDB], CHECK_EXPIRATION = OFF, CHECK_POLICY = OFF;
GO
CREATE LOGIN [CONTOSO\SQL-Readers] FROM WINDOWS WITH DEFAULT_DATABASE = [SalesDB];
GO
USE [SalesDB]
GO
CREATE USER [svc_reporting] FOR LOGIN [svc_reporting] WITH DEFAULT_SCHEMA = [dbo];
CREATE USER [CONTOSO\SQL-Readers] FOR LOGIN [CONTOSO\SQL-Readers];
CREATE ROLE [ReportingRole] AUTHORIZATION [dbo];
GO
ALTER ROLE [db_datareader] ADD MEMBER [CONTOSO\SQL-Readers];
ALTER ROLE [ReportingRole] ADD MEMBER [svc_reporting];
GRANT SELECT ON SCHEMA::[Sales] TO [ReportingRole];
GRANT EXECUTE ON SCHEMA::dbo TO [ReportingRole];
GRANT VIEW DEFINITION TO [ReportingRole];
DENY SELECT ON OBJECT::dbo.EmployeePayHistory TO [ReportingRole];
DENY UPDATE, DELETE ON SCHEMA::[Sales] TO [ReportingRole];
REVOKE INSERT ON dbo.SalesOrderStaging FROM [ReportingRole];
GO
EXECUTE AS USER = N'svc_reporting';
SELECT USER_NAME() AS DbUser, SUSER_NAME() AS LoginName;
SELECT * FROM fn_my_permissions(N'Sales', N'SCHEMA');
REVERT;
GO
