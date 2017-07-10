DECLARE  @ReportServerDBConfig NVARCHAR(MAX)
		,@ReportServerRecoveryModelConfig NVARCHAR(MAX)
		,@ReportServerTempDBRecoveryModelConfig NVARCHAR(MAX)
		,@ReportServerMaxSizeMB INT
		,@ReportServerTempDBMaxSizeMB INT
		
/***********************************************************************************************************************************************
-- Set ReportServer database recovery model
***********************************************************************************************************************************************/

SET @ReportServerRecoveryModelConfig = '
	USE ReportServer
	ALTER DATABASE ReportServer SET RECOVERY SIMPLE
'
BEGIN TRY
	EXECUTE SP_EXECUTESQL @ReportServerRecoveryModelConfig
	GOTO ReportServerTempDBRecoveryModel;
END TRY
BEGIN CATCH
    GOTO ExitError;
END CATCH;

ReportServerTempDBRecoveryModel:
SET @ReportServerTempDBRecoveryModelConfig = '
	USE ReportServerTempDB
	ALTER DATABASE ReportServerTempDB SET RECOVERY SIMPLE
'
BEGIN TRY
	EXECUTE SP_EXECUTESQL @ReportServerTempDBRecoveryModelConfig
	GOTO ReportServerConfig;
END TRY
BEGIN CATCH
    GOTO ExitError;
END CATCH;

/***********************************************************************************************************************************************
-- Set ReportServer and ReportServerTempDB database files growth
***********************************************************************************************************************************************/

ReportServerConfig:
SELECT
@ReportServerMaxSizeMB = FLOOR(@ReportServerMaxSizeGB * 1024),
@ReportServerTempDBMaxSizeMB = FLOOR(@ReportServerTempDBMaxSizeGB * 1024)

SET @ReportServerDBConfig = '
	USE ReportServer
	ALTER DATABASE [ReportServer] MODIFY FILE (
		NAME = ReportServer,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerMaxSizeMB) +'MB
	)
	ALTER DATABASE [ReportServer] MODIFY FILE (
		NAME = ReportServer_log,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerMaxSizeMB) +'MB
	)	
	
	USE ReportServerTempDB
	ALTER DATABASE [ReportServerTempDB] MODIFY FILE (
		NAME = ReportServerTempDB,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerTempDBMaxSizeMB) +'MB
	)
	ALTER DATABASE [ReportServerTempDB] MODIFY FILE (
		NAME = ReportServerTempDB_log,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerTempDBMaxSizeMB) +'MB
	)	
'
BEGIN TRY
	EXECUTE SP_EXECUTESQL @ReportServerDBConfig
	GOTO ScriptSuccess;
END TRY
BEGIN CATCH
    GOTO ExitError;
END CATCH;

/************************************************************************************************************************
    Exits and End of Script:
************************************************************************************************************************/
ExitError:
PRINT N'Error';
SET @ReturnValue = 1
GOTO EndScript;

ScriptSuccess:
PRINT N'Success';
SET @ReturnValue = 0
GOTO EndScript;

EndScript:
PRINT @ReturnValue