-- expect: Script(TsqlUseStatement | TsqlTransactionStatement,TsqlDeclareStatement,SelectStatement,TsqlIfStatement,TsqlDeclareStatement,TsqlExecuteStatement,TsqlIfStatement,TsqlExecuteStatement,TsqlIfStatement,TsqlExecuteStatement,TsqlIfStatement,TsqlExecuteStatement,TsqlIfStatement,TsqlExecuteStatement,TsqlIfStatement,TsqlTransactionStatement,TsqlGotoStatement,TsqlLabelStatement,TsqlIfStatement,TsqlLabelStatement)
USE [msdb]
GO

/****** Object:  Job [Nightly - Sales ETL]    Script Date: 3/14/2024 9:12:03 AM ******/
BEGIN TRANSACTION
DECLARE @ReturnCode INT
SELECT @ReturnCode = 0
/****** Object:  JobCategory [[Uncategorized (Local)]]    Script Date: 3/14/2024 9:12:03 AM ******/
IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'[Uncategorized (Local)]' AND category_class=1)
BEGIN
EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'[Uncategorized (Local)]'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

END

DECLARE @jobId BINARY(16)
EXEC @ReturnCode =  msdb.dbo.sp_add_job @job_name=N'Nightly - Sales ETL', 
		@enabled=1, 
		@notify_level_eventlog=0, 
		@notify_level_email=2, 
		@description=N'Loads sales data from staging and rebuilds aggregates.', 
		@category_name=N'[Uncategorized (Local)]', 
		@owner_login_name=N'sa', 
		@notify_email_operator_name=N'DBA Team', @job_id = @jobId OUTPUT
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
/****** Object:  Step [Load staging]    Script Date: 3/14/2024 9:12:03 AM ******/
EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'Load staging', 
		@step_id=1, 
		@on_success_action=1, 
		@on_fail_action=2, 
		@retry_attempts=2, 
		@retry_interval=5, 
		@subsystem=N'TSQL', 
		@command=N'EXEC staging.usp_LoadSales @BatchDate = NULL;', 
		@database_name=N'SalesDB', 
		@flags=0
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = 1
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_add_jobschedule @job_id=@jobId, @name=N'Daily 02:00', 
		@enabled=1, 
		@freq_type=4, 
		@freq_interval=1, 
		@freq_subday_type=1, 
		@active_start_date=20240314, 
		@active_end_date=99991231, 
		@active_start_time=20000, 
		@active_end_time=235959
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = N'(local)'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
COMMIT TRANSACTION
GOTO EndSave
QuitWithRollback:
    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION
EndSave:
GO
